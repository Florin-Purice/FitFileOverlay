using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SkiaSharp;

namespace FitFileOverlay.Overlay;

public class FitOverlayProcessor
{
    private static readonly float _drawAreaPercent = 0.9f;
    private readonly FitMessages? fitMessages;
    private int selectedLthr;
    private GPSPoint gpsCenterPoint;
    private double gpsAreaSize;
    private SKBitmap? gpsOverlayBitmap;

    public FitOverlayProcessor(string filePath)
    {
        try
        {
            fitMessages = null;
            FileLTHR = 0; selectedLthr = 0;
            gpsOverlayBitmap = null;
            using (FileStream fitFileStream = new(filePath, FileMode.Open))
            {
                Decode decoder = new();
                FitListener fitListener = new();
                decoder.MesgEvent += fitListener.OnMesg;
                decoder.Read(fitFileStream);
                fitMessages = fitListener.FitMessages;
            }
            RecordsCount = fitMessages.RecordMesgs.Count;
            if (RecordsCount < 1)
            {
                IsValid = false;
                return;
            }
            //Determine activity length
            DateTime? startTime = fitMessages.RecordMesgs.FirstOrDefault()?.GetTimestamp();
            DateTime? stopTime = fitMessages.RecordMesgs.LastOrDefault()?.GetTimestamp();
            uint? activityLengthSec = stopTime?.GetTimeStamp() - startTime?.GetTimeStamp();
            if (activityLengthSec == null | activityLengthSec < 0)
                activityLengthSec = 0;
            string hourPart = (activityLengthSec / 3600) > 0 ? $"{activityLengthSec / 3600}h " : "";
            string minutePart = ((activityLengthSec % 3600) / 60) > 0 ? $"{(activityLengthSec % 3600) / 60}m " : "";
            string secondPart = (activityLengthSec % 60) > 0 ? $"{(activityLengthSec % 60)}s" : "";
            ZonesTargetMesg? zonesTargetMesg = fitMessages.ZonesTargetMesgs.FirstOrDefault();
            FileLTHR = zonesTargetMesg?.GetThresholdHeartRate() ?? 160;
            ActivityDurationString = $"{hourPart}{minutePart}{secondPart}";
            IsValid = true;
        }
        catch (Exception ex)
        {
            fitMessages = null;
            ErrorMessage = ex.Message;
            IsValid = false;
        }
        finally
        {
            gpsCenterPoint = new(0, 0);
        }
    }

    public delegate void LogProgressDelegate(FitOverlayProcessor sender, LogProgressEventArgs e);
    public event LogProgressDelegate? LogProgress;

    public bool IsValid { get; private set; }
    public int RecordsCount { get; private set; }
    public int FileLTHR { get; }
    public string ActivityDurationString { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;

    #region PUBLIC METHODS
    public async Task ExportVideo(string outputPath, CancellationToken? cancellationToken = null)
    {
        //Determine LTHR to use
        if (Settings.Get<bool>("UseFileLTHR"))
            selectedLthr = FileLTHR;
        else
        {
            selectedLthr = Settings.Get<int>("CustomLTHR");
            if (selectedLthr <= 0)
                selectedLthr = FileLTHR;
        }
        //Start video generation in background
        //Generate video frames and encode video using FFMpegCore
        IEnumerable<IVideoFrame> frames = GenerateFrames();
        RawVideoPipeSource framesSource = new(frames)
        {
            FrameRate = Settings.Get<uint>("FPS")
        };
        await FFMpegArguments.FromPipeInput(framesSource)
            .OutputToFile(outputPath, true, opt => opt
                .WithFramerate(Settings.Get<uint>("FPS"))
                .WithVideoCodec("prores_ks")
                .ForcePixelFormat("yuva444p10le")
                .WithCustomArgument("-profile:v 4444")
                .WithConstantRateFactor(17))
            .CancellableThrough(cancellationToken ?? new CancellationToken())
            .ProcessAsynchronously(throwOnError: true);
    }

    public SKBitmap? GetSnapshotAtRecord(int recordIndex, int interpolationIndex = 0)
    {
        if (fitMessages == null)
            return null;
        if (Settings.Get<bool>("UseFileLTHR"))
            selectedLthr = FileLTHR;
        else
        {
            selectedLthr = Settings.Get<int>("CustomLTHR");
            if (selectedLthr <= 0)
                selectedLthr = FileLTHR;
        }
        //clear existing GPS overlay bitmap in case color settings have changed
        gpsOverlayBitmap?.Dispose();
        gpsOverlayBitmap = null;

        List<GPSPoint> gpsPointsList = [];
        int gpsOverlayWidth = Settings.Get<int>("GPSOverlayWidth");
        int gpsOverlayHeight = Settings.Get<int>("GPSOverlayHeight");
        SKBitmap previousPathOverlay = new(gpsOverlayWidth, gpsOverlayHeight);
        SKBitmap[] frames = GenerateFramesFromRecord(recordIndex, ref gpsPointsList, ref previousPathOverlay, fitMessages.RecordMesgs[recordIndex], recordIndex > 0 ? fitMessages.RecordMesgs[recordIndex - 1] : null);
        if (interpolationIndex < frames.Length)
            return frames[interpolationIndex];
        else
            return frames.Last();
    }
    #endregion

    #region PRIVATE METHODS
    private static GPSPoint FitPointToGpsPoint(GPSPoint fitGpsPoint)
    {
        double lat = fitGpsPoint.Latitude * (180d / Int32.MaxValue);
        double lon = fitGpsPoint.Longitude * (180d / Int32.MaxValue);
        return new GPSPoint(lat, lon);
    }

    private static GPSPoint GPSToMeters(GPSPoint gpsPointA, GPSPoint gpsPointB)
    {
        double LatDegToMeter = 111320;
        double EarthCumference = 40075000;
        double latDiff = gpsPointA.Latitude - gpsPointB.Latitude;
        double lonDiff = gpsPointA.Longitude - gpsPointB.Longitude;
        //convert degree difference to meters; compensate longitude conversion by multiplying with cosine of latitude
        double x = latDiff * LatDegToMeter;
        double y = lonDiff * (EarthCumference * Math.Cos(gpsPointA.Latitude * (Math.PI / 180)) / 360);
        return new GPSPoint(x, y);
    }

    private static GPSPoint GPSToXyAt(GPSPoint gpsPoint, double gpsAreaSize, double drawSize = 1f)
    {
        double scale = drawSize / gpsAreaSize;
        double lat = gpsPoint.Latitude;
        double lon = gpsPoint.Longitude;
        //scale to draw area and invert y axis
        lat *= -scale * _drawAreaPercent;
        lon *= scale * _drawAreaPercent;
        //translate to draw area center
        lat += drawSize / 2;
        lon += drawSize / 2;
        return new GPSPoint(lat, lon);
    }

    private void OnLogProgress(float progress, TimeSpan elapsed, TimeSpan eta)
    {
        LogProgress?.Invoke(this, new LogProgressEventArgs(progress, elapsed, eta));
    }

    private SKBitmap CreateGPSOverlay(int recordIndex, int interpolationIndex, ref List<GPSPoint> previousList, ref SKBitmap previousPathOverlay)
    {
        SolidColorBrush primaryColorX = Settings.Get<bool>("IsPrimaryOverlay") ? Settings.Get<SolidColorBrush>("PrimaryColor") : Settings.Get<SolidColorBrush>("AltPrimaryColor");
        SKColor primaryColor = new(primaryColorX.Color.R, primaryColorX.Color.G, primaryColorX.Color.B, primaryColorX.Color.A);
        SolidColorBrush secondaryColorX = Settings.Get<bool>("IsPrimaryOverlay") ? Settings.Get<SolidColorBrush>("SecondaryColor") : Settings.Get<SolidColorBrush>("AltSecondaryColor");
        SKColor secondaryColor = new(secondaryColorX.Color.R, secondaryColorX.Color.G, secondaryColorX.Color.B, secondaryColorX.Color.A);
        // Create a bitmap with specified dimensions
        int gpsOverlayWidth = Settings.Get<int>("GPSOverlayWidth");
        int gpsOverlayHeight = Settings.Get<int>("GPSOverlayHeight");
        SKBitmap bitmap = new(gpsOverlayWidth, gpsOverlayHeight);
        if (fitMessages == null || fitMessages.RecordMesgs.Count <= recordIndex)
            return bitmap;
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        if (gpsOverlayBitmap == null)
            CreateBaseGPSOverlay();
        canvas.DrawBitmap(gpsOverlayBitmap, 0, 0, SKSamplingOptions.Default);
        uint fps = Settings.Get<uint>("FPS");
        int previousListCount = previousList.Count;
        int fadeDurationFrames = Settings.Get<int>("GpsFadeDurationSeconds") * (int)fps;
        List<GPSPoint> gpsPoints = [];
        for (int ri = recordIndex; ri > 0; --ri)
        {
            RecordMesg recordB = fitMessages.RecordMesgs[ri];
            RecordMesg recordA = fitMessages.RecordMesgs[ri - 1];
            if (recordA.GetPositionLat().HasValue && recordA.GetPositionLong().HasValue && recordB.GetPositionLat().HasValue && recordB.GetPositionLong().HasValue)
            {
                //Interpolate GPS points
                GPSPoint fitGpsPointA = new(recordA.GetPositionLat().GetValueOrDefault(), recordA.GetPositionLong().GetValueOrDefault());
                GPSPoint fitGpsPointB = new(recordB.GetPositionLat().GetValueOrDefault(), recordB.GetPositionLong().GetValueOrDefault());
                int latStep = (int)((fitGpsPointB.Latitude - fitGpsPointA.Latitude) / fps);
                int lonStep = (int)((fitGpsPointB.Longitude - fitGpsPointA.Longitude) / fps);
                GPSPoint[] interpolatedPoints = new GPSPoint[fps];
                for (int i = 1; i <= fps; ++i)
                    interpolatedPoints[i - 1] = new GPSPoint(fitGpsPointA.Latitude + latStep * i, fitGpsPointA.Longitude + lonStep * i);
                for (int i = interpolatedPoints.Length - 1; i >= 0; --i)
                    if (ri < recordIndex || i <= interpolationIndex)
                    {
                        gpsPoints.Add(interpolatedPoints[i]);
                        //if we are given a list of previous calculated points, compute only current point
                        if (previousListCount != 0)
                            break;
                    }
            }
            if (previousListCount != 0 && gpsPoints.Count > 0)
            {
                gpsPoints.AddRange(previousList);
                break;
            }
        }
        previousList = gpsPoints;
        float drawAreaSize = Math.Min(gpsOverlayWidth, gpsOverlayHeight);
        float gpsLineWidth = Settings.Get<float>("GpsLineWidth");
        skPaint.StrokeWidth = gpsLineWidth * 2f;
        //Draw only new points since last frame and store in a separate bitmap to save processing power
        SKCanvas previousPathCanvas = new(previousPathOverlay);
        for (int frameIndex = 1; frameIndex < gpsPoints.Count && frameIndex <= gpsPoints.Count - previousListCount; ++frameIndex)
        {
            GPSPoint pointB = GPSToXyAt(GPSToMeters(FitPointToGpsPoint(gpsPoints[frameIndex]), gpsCenterPoint), gpsAreaSize, drawAreaSize);
            GPSPoint pointA = GPSToXyAt(GPSToMeters(FitPointToGpsPoint(gpsPoints[frameIndex - 1]), gpsCenterPoint), gpsAreaSize, drawAreaSize);
            skPaint.Color = primaryColor;
            previousPathCanvas.DrawLine((float)pointA.Longitude, (float)pointA.Latitude, (float)pointB.Longitude, (float)pointB.Latitude, skPaint);
            //smooth corners by drawing circles at points
            previousPathCanvas.DrawCircle((float)pointA.Longitude, (float)pointA.Latitude, gpsLineWidth, skPaint);
            previousPathCanvas.DrawCircle((float)pointB.Longitude, (float)pointB.Latitude, gpsLineWidth, skPaint);
        }
        canvas.DrawBitmap(previousPathOverlay, 0, 0, SKSamplingOptions.Default);
        //Draw fading path
        for (int frameIndex = Math.Min(gpsPoints.Count - 1, fadeDurationFrames); frameIndex > 0; --frameIndex)
        {
            float fadePercent = 1f - ((float)frameIndex / fadeDurationFrames);
            GPSPoint pointB = GPSToXyAt(GPSToMeters(FitPointToGpsPoint(gpsPoints[frameIndex]), gpsCenterPoint), gpsAreaSize, drawAreaSize);
            GPSPoint pointA = GPSToXyAt(GPSToMeters(FitPointToGpsPoint(gpsPoints[frameIndex - 1]), gpsCenterPoint), gpsAreaSize, drawAreaSize);
            skPaint.Color = new SKColor(
                (byte)(fadePercent * secondaryColor.Red + (1f - fadePercent) * primaryColor.Red),
                (byte)(fadePercent * secondaryColor.Green + (1f - fadePercent) * primaryColor.Green),
                (byte)(fadePercent * secondaryColor.Blue + (1f - fadePercent) * primaryColor.Blue),
                (byte)(fadePercent * secondaryColor.Alpha + (1f - fadePercent) * primaryColor.Alpha));
            canvas.DrawLine((float)pointA.Longitude, (float)pointA.Latitude, (float)pointB.Longitude, (float)pointB.Latitude, skPaint);
            //smooth corners by drawing circles at points
            canvas.DrawCircle((float)pointA.Longitude, (float)pointA.Latitude, gpsLineWidth, skPaint);
            canvas.DrawCircle((float)pointB.Longitude, (float)pointB.Latitude, gpsLineWidth, skPaint);
        }
        //Draw current position with a circle
        if (gpsPoints.Count > 0)
        {
            GPSPoint currentPoint = GPSToXyAt(GPSToMeters(FitPointToGpsPoint(gpsPoints.First()), gpsCenterPoint), gpsAreaSize, drawAreaSize);
            skPaint.Color = secondaryColor;
            canvas.DrawCircle((float)currentPoint.Longitude, (float)currentPoint.Latitude, gpsLineWidth * 2, skPaint);
        }
        return bitmap;
    }

    private void CreateBaseGPSOverlay()
    {
        SolidColorBrush gpsOutlineColorX = Settings.Get<bool>("IsPrimaryOverlay") ? Settings.Get<SolidColorBrush>("GpsOutlineColor") : Settings.Get<SolidColorBrush>("AltGpsOutlineColor");
        SKColor gpsOutlineColor = new(gpsOutlineColorX.Color.R, gpsOutlineColorX.Color.G, gpsOutlineColorX.Color.B, gpsOutlineColorX.Color.A);
        // Create a bitmap with specified dimensions
        int gpsOverlayWidth = Settings.Get<int>("GPSOverlayWidth");
        int gpsOverlayHeight = Settings.Get<int>("GPSOverlayHeight");
        gpsOverlayBitmap = new SKBitmap(gpsOverlayWidth, gpsOverlayHeight);
        using SKCanvas canvas = new(gpsOverlayBitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        //Make a list of all GPS point and calculate the extremities
        List<GPSPoint> gpsPoints = [];
        double minLat, minLon, maxLat, maxLon;
        minLat = minLon = double.MaxValue;
        maxLat = maxLon = double.MinValue;
        if (fitMessages != null)
            foreach (RecordMesg record in fitMessages.RecordMesgs)
            {
                if (record.GetPositionLat().HasValue && record.GetPositionLong().HasValue)
                {
                    GPSPoint fitGpsPoint = new(record.GetPositionLat().GetValueOrDefault(), record.GetPositionLong().GetValueOrDefault());
                    GPSPoint gpsPoint = FitPointToGpsPoint(fitGpsPoint);
                    gpsPoints.Add(gpsPoint);
                    if (gpsPoint.Longitude < minLon)
                        minLon = gpsPoint.Longitude;
                    if (gpsPoint.Longitude > maxLon)
                        maxLon = gpsPoint.Longitude;
                    if (gpsPoint.Latitude < minLat)
                        minLat = gpsPoint.Latitude;
                    if (gpsPoint.Latitude > maxLat)
                        maxLat = gpsPoint.Latitude;
                }
            }
        //Determine the centerpoint and convert GPS points to meters relative to centerpoint, while recalculating extremities in meters
        gpsCenterPoint = new GPSPoint((minLat + maxLat) / 2, (minLon + maxLon) / 2);
        minLat = minLon = double.MaxValue;
        maxLat = maxLon = double.MinValue;
        for (int i = 0; i < gpsPoints.Count; ++i)
        {
            gpsPoints[i] = GPSToMeters(gpsPoints[i], gpsCenterPoint);
            if (gpsPoints[i].Longitude < minLon)
                minLon = gpsPoints[i].Longitude;
            if (gpsPoints[i].Longitude > maxLon)
                maxLon = gpsPoints[i].Longitude;
            if (gpsPoints[i].Latitude < minLat)
                minLat = gpsPoints[i].Latitude;
            if (gpsPoints[i].Latitude > maxLat)
                maxLat = gpsPoints[i].Latitude;
        }
        gpsAreaSize = Math.Max(maxLon - minLon, maxLat - minLat);
        //Determine draw area size and draw lines between GPS points
        float drawAreaSize = Math.Min(gpsOverlayWidth, gpsOverlayHeight);
        float gpsLineWidth = Settings.Get<float>("GpsLineWidth");
        skPaint.StrokeWidth = gpsLineWidth;
        for (int i = 0; i < gpsPoints.Count - 1; ++i)
        {
            GPSPoint drawPointA = GPSToXyAt(gpsPoints[i], gpsAreaSize, drawAreaSize);
            GPSPoint drawPointB = GPSToXyAt(gpsPoints[i + 1], gpsAreaSize, drawAreaSize);
            skPaint.Color = gpsOutlineColor;
            canvas.DrawLine((float)drawPointA.Longitude, (float)drawPointA.Latitude, (float)drawPointB.Longitude, (float)drawPointB.Latitude, skPaint);
            //smooth corners by drawing circles at points
            canvas.DrawCircle((float)drawPointA.Longitude, (float)drawPointA.Latitude, gpsLineWidth / 2f, skPaint);
            canvas.DrawCircle((float)drawPointB.Longitude, (float)drawPointB.Latitude, gpsLineWidth / 2f, skPaint);
        }
    }

    private IEnumerable<IVideoFrame> GenerateFrames()
    {
        //clear existing GPS overlay bitmap in case color settings have changed
        gpsOverlayBitmap?.Dispose();
        gpsOverlayBitmap = null;
        System.DateTime startTime = System.DateTime.Now;
        TimeSpan elapsed;
        List<GPSPoint> gpsPointsList = [];
        int gpsOverlayWidth = Settings.Get<int>("GPSOverlayWidth");
        int gpsOverlayHeight = Settings.Get<int>("GPSOverlayHeight");
        SKBitmap previousPathOverlay = new(gpsOverlayWidth, gpsOverlayHeight);
        for (int i = 0; i < fitMessages!.RecordMesgs.Count; ++i)
        {
            //Log progress
            float progress = (float)i / fitMessages.RecordMesgs.Count;
            elapsed = System.DateTime.Now - startTime;
            TimeSpan remaining = TimeSpan.FromSeconds(progress > 0 ? elapsed.TotalSeconds * (1d / progress - 1d) : 0d);
            OnLogProgress(progress, elapsed, remaining);
            //Generate frames for current record, interpolating values from previous record if available
            SKBitmap[] skframes = GenerateFramesFromRecord(recordIndex: i, previousPoints: ref gpsPointsList, previousPathOverlay: ref previousPathOverlay, record: fitMessages.RecordMesgs[i], previous: i > 0 ? fitMessages.RecordMesgs[i - 1] : null);
            foreach (SKBitmap skFrame in skframes)
            {
                yield return new SKBitmapFrame(skFrame);
            }
        }
    }

    private SKBitmap[] GenerateFramesFromRecord(int recordIndex, ref List<GPSPoint> previousPoints, ref SKBitmap previousPathOverlay, RecordMesg record, RecordMesg? previous)
    {
        if (previous == null)
        {
            DateTime timestamp = record.GetTimestamp();
            float? distance = record.GetDistance();
            float? enhancedSpeed = record.GetEnhancedSpeed();
            int? heartRate = record.GetHeartRate();
            //calculate pace based on enhanced speed
            int secPerKm = 0;
            if (enhancedSpeed.HasValue)
                secPerKm = (int)(1000 / enhancedSpeed);
            string paceString;
            if (secPerKm < 1 || secPerKm > 3600)
                paceString = "--'--\"";
            else
                paceString = $"{secPerKm / 60}'{secPerKm % 60:D2}\"";
            return [GenerateFrame(timestamp, paceString, heartRate ?? 0, distance ?? 0)];
        }
        else
        {
            uint timeBetweenRecords = record.GetTimestamp().GetTimeStamp() - previous.GetTimestamp().GetTimeStamp();
            uint frameCount = Settings.Get<uint>("FPS") * timeBetweenRecords;
            float timestampStep = ((float)(record.GetTimestamp().GetTimeStamp() - previous.GetTimestamp().GetTimeStamp())) / frameCount;
            float distanceStep = ((record.GetDistance() ?? 0f) - (previous.GetDistance() ?? 0f)) / frameCount;
            float enhancedSpeedStep = ((record.GetEnhancedSpeed() ?? 0f) - (previous.GetEnhancedSpeed() ?? 0f)) / frameCount;
            float heartRateStep = (float)((record.GetHeartRate() ?? 0) - (previous.GetHeartRate() ?? 0)) / frameCount;
            SKBitmap[] frames = new SKBitmap[frameCount];
            //interpolate values for each frame
            for (int i = 1; i <= frameCount; ++i)
            {
                DateTime timestamp = new((previous.GetTimestamp().GetTimeStamp() + (uint)(timestampStep * i)));
                float distance = (previous.GetDistance() ?? 0f) + distanceStep * i;
                float enhancedSpeed = (previous.GetEnhancedSpeed() ?? 0f) + enhancedSpeedStep * i;
                int heartRate = (int)((previous.GetHeartRate() ?? 0) + heartRateStep * i);
                //calculate pace based on enhanced speed
                int secPerKm = (int)(1000 / enhancedSpeed);
                string paceString;
                if (secPerKm < 1 || secPerKm > 3600)
                    paceString = "--'--\"";
                else
                    paceString = $"{secPerKm / 60}'{secPerKm % 60:D2}\"";
                SKBitmap frame = GenerateFrame(timestamp, paceString, heartRate, distance);
                //add gps overlay
                SKBitmap gpsOverlay = CreateGPSOverlay(recordIndex, i - 1, ref previousPoints, ref previousPathOverlay);
                SKCanvas canvas = new(frame);
                canvas.DrawBitmap(gpsOverlay, Settings.Get<int>("OverlayWidth") - Settings.Get<int>("GPSOverlayWidth"), 0, SKSamplingOptions.Default);
                frames[i - 1] = frame;
            }
            return frames;
        }
    }

    private SKBitmap GenerateFrame(DateTime timestamp, string pace, int heartRate, float distance)
    {
        float lineSpacing = Settings.Get<int>("LineSpacingPixels");
        float fontSizeSmall = Settings.Get<float>("FontSizeSmall");
        float fontSizeBig = Settings.Get<float>("FontSizeBig");
        string labelFontFamily = Settings.Get<string>("LabelFontFamily");
        string valueFontFamily = Settings.Get<string>("ValueFontFamily");
        SKTypeface valueTypeface = SKTypeface.FromFamilyName(valueFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
        SKFont labelFont = new(SKTypeface.FromFamilyName(labelFontFamily), fontSizeSmall);
        SKFont valueFont = new(valueTypeface, fontSizeBig);
        SKFont unitFont = new(valueTypeface, fontSizeSmall);
        SKFont timestampFont = new(valueTypeface, fontSizeSmall);
        string hrValue = heartRate.ToString();
        string distanceValue = (distance / 1000).ToString("0.00");
        string timestampText = timestamp.ToString();
        SolidColorBrush hrColorX = Settings.Get<SolidColorBrush[]>("ZoneBrushes")[GetHeartRateZone(heartRate)];
        hrColorX.Dispatcher?.Invoke(() => hrColorX.Freeze());
        SKColor hrColor = new(hrColorX.Color.R, hrColorX.Color.G, hrColorX.Color.B, hrColorX.Color.A);
        SolidColorBrush primaryColorX = Settings.Get<SolidColorBrush>("PrimaryColor");
        SKColor primaryColor = new(primaryColorX.Color.R, primaryColorX.Color.G, primaryColorX.Color.B, primaryColorX.Color.A);
        SolidColorBrush secondaryColorX = Settings.Get<SolidColorBrush>("SecondaryColor");
        SKColor secondaryColor = new(secondaryColorX.Color.R, secondaryColorX.Color.G, secondaryColorX.Color.B, secondaryColorX.Color.A);
        // Create a bitmap with specified dimensions
        int overlayWidth = Settings.Get<int>("OverlayWidth");
        int overlayHeight = Settings.Get<int>("OverlayHeight");
        SKBitmap bitmap = new(overlayWidth, overlayHeight);
        // Use SKCanvas to draw on the bitmap
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        // Define rectangles for text placement and draw text
        //pace
        string paceLabel = Settings.Get<string>("PaceLabel");
        string paceUnit = Settings.Get<string>("PaceUnit");
        skPaint.Color = primaryColor;
        labelFont.MeasureText(paceLabel, out SKRect paceLabelSize, skPaint);
        paceLabelSize.Offset(0, fontSizeSmall + lineSpacing);
        canvas.DrawText(paceLabel, paceLabelSize.Left, paceLabelSize.Bottom, SKTextAlign.Left, labelFont, skPaint);
        valueFont.MeasureText(pace, out SKRect paceValueSize, skPaint);
        paceValueSize.Offset(0, paceLabelSize.Bottom + fontSizeBig - paceValueSize.Bottom/*fix font irregularities*/ + lineSpacing);
        canvas.DrawText(pace, paceValueSize.Left, paceValueSize.Bottom, SKTextAlign.Left, valueFont, skPaint);
        skPaint.Color = secondaryColor;
        unitFont.MeasureText(paceUnit, out SKRect paceUnitSize, skPaint);
        paceUnitSize.Offset(paceValueSize.Right, paceValueSize.Bottom);
        canvas.DrawText(paceUnit, paceUnitSize.Left, paceUnitSize.Bottom, SKTextAlign.Left, unitFont, skPaint);
        //heart rate
        string hrLabel = Settings.Get<string>("HrLabel");
        string hrUnit = Settings.Get<string>("HrUnit");
        skPaint.Color = primaryColor;
        labelFont.MeasureText(hrLabel, out SKRect hrLabelSize, skPaint);
        hrLabelSize.Offset(0, (float)overlayHeight / 3 + fontSizeSmall + lineSpacing);
        canvas.DrawText(hrLabel, hrLabelSize.Left, hrLabelSize.Bottom, SKTextAlign.Left, labelFont, skPaint);
        skPaint.Color = hrColor;
        valueFont.MeasureText(hrValue, out SKRect hrValueSize, skPaint);
        hrValueSize.Offset(0, hrLabelSize.Bottom + fontSizeBig - hrValueSize.Bottom/*fix font irregularities*/ + lineSpacing);
        canvas.DrawText(hrValue, hrValueSize.Left, hrValueSize.Bottom, SKTextAlign.Left, valueFont, skPaint);
        skPaint.Color = secondaryColor;
        unitFont.MeasureText(hrUnit, out SKRect hrUnitSize, skPaint);
        hrUnitSize.Offset(hrValueSize.Right, hrValueSize.Bottom);
        canvas.DrawText(hrUnit, hrUnitSize.Left, hrUnitSize.Bottom, SKTextAlign.Left, unitFont, skPaint);
        //distance
        string distanceLabel = Settings.Get<string>("DistanceLabel");
        string distanceUnit = Settings.Get<string>("DistanceUnit");
        skPaint.Color = primaryColor;
        labelFont.MeasureText(distanceLabel, out SKRect distanceLabelSize, skPaint);
        distanceLabelSize.Offset(0, (float)overlayHeight * 2 / 3 + fontSizeSmall + lineSpacing);
        canvas.DrawText(distanceLabel, distanceLabelSize.Left, distanceLabelSize.Bottom, SKTextAlign.Left, labelFont, skPaint);
        valueFont.MeasureText(distanceValue, out SKRect distanceValueSize, skPaint);
        distanceValueSize.Offset(0, distanceLabelSize.Bottom + fontSizeBig - distanceValueSize.Bottom/*fix font irregularities*/ + lineSpacing);
        canvas.DrawText(distanceValue, distanceValueSize.Left, distanceValueSize.Bottom, SKTextAlign.Left, valueFont, skPaint);
        skPaint.Color = secondaryColor;
        unitFont.MeasureText(distanceUnit, out SKRect distanceUnitSize, skPaint);
        distanceUnitSize.Offset(distanceValueSize.Right, distanceValueSize.Bottom);
        canvas.DrawText(distanceUnit, distanceUnitSize.Left, distanceUnitSize.Bottom, SKTextAlign.Left, unitFont, skPaint);
        //timestamp
        skPaint.Color = primaryColor;
        timestampFont.MeasureText(timestampText, out SKRect timestampSize, skPaint);
        timestampSize.Offset(0, overlayHeight - lineSpacing);
        canvas.DrawText(timestampText, timestampSize.Left, timestampSize.Bottom, SKTextAlign.Left, timestampFont, skPaint);

        return bitmap;
    }

    private int GetHeartRateZone(int heartRate)
    {
        float[] zoneMaxPercent = Settings.Get<float[]>("ZoneMaxPercent");
        float percentLthr = (float)heartRate / selectedLthr;
        for (int i = 0; i < zoneMaxPercent.Length; ++i)
            if (percentLthr <= zoneMaxPercent[i])
                return i;
        return 0;
    }
    #endregion

    public record GPSPoint(double Latitude, double Longitude);

    public class LogProgressEventArgs(float progress, TimeSpan elapsed, TimeSpan remaining) : EventArgs
    {
        public float Progress { get; set; } = progress;
        public TimeSpan Elapsed { get; set; } = elapsed;
        public TimeSpan Remaining { get; set; } = remaining;
    }
}
