using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;

using FFMpegCore;
using FFMpegCore.Pipes;

using SkiaSharp;

using static FitFileOverlay.Overlay.DataFieldRenderer;
using static FitFileOverlay.Overlay.PathRenderer;

namespace FitFileOverlay.Overlay;

public class OverlayProcessor
{
    private readonly FitFile _fitFile;

    public OverlayProcessor(string sourceFilePath)
    {
        _fitFile = new FitFile(sourceFilePath);
        if (!_fitFile.IsValid)
            throw new Exception(_fitFile.ErrorMessage);
    }

    public async Task ExportVideo(OverlaySettings settings, string outFileName, CancellationToken? cancellationToken = null)
    {
        //insert interpolated records if needed
        List<IActivityRecord> records;
        if (settings.FPS > 1)
            records = InsertInterpolatedRecord(_fitFile.Records, settings.FPS);
        else
            records = _fitFile.Records;
        //create list of unitary screenspace gps points
        List<(double x, double y)?> normalizedPoints = ProcessGpsPoints(records, out double gpsAspectRatio);
        //Generate video frames and encode video using FFMpegCore
        IEnumerable<IVideoFrame> frames = CreateVideoFrames(settings, records, normalizedPoints, gpsAspectRatio);
        RawVideoPipeSource framesSource = new(frames)
        {
            FrameRate = settings.FPS,
        };
        await FFMpegArguments.FromPipeInput(framesSource)
            .OutputToFile(outFileName, true, opt => opt
                .WithFramerate(settings.FPS)
                .WithVideoCodec("prores_ks")
                .ForcePixelFormat("yuva444p10le")
                .WithCustomArgument("-profile:v 4444")
                .WithConstantRateFactor(17))
            .CancellableThrough(cancellationToken ?? new CancellationToken())
            .ProcessAsynchronously(throwOnError: true);
    }

    private static IEnumerable<IVideoFrame> CreateVideoFrames(OverlaySettings settings, List<IActivityRecord> records, List<(double x, double y)?> normalizedPoints, double gpsAspectRatio)
    {
        //define layout
        int dataOverlayWidth = (settings.Size.Width - settings.GPSOverlayWidth) / settings.DataOverlayColumnCount;
        int dataOverlayHeight = (int)(settings.FontSizeSmall + settings.FontSizeBig + settings.LineSpacing + settings.DataOverlayVerticalSpacing);
        int dataFieldCount = 4;
        int dataFieldsPerColumn = (int)Math.Ceiling((double)dataFieldCount / settings.DataOverlayColumnCount);
        int gpsOverlayWidth = settings.GPSOverlayWidth;
        int gpsOverlayHeight = settings.Size.Height;
        float gpsDrawAreaPadding = settings.GpsLineWidth;//add some padding so the points on the border dont get cut off
        double gpsDrawAreaAspectRatio = (double)(gpsOverlayWidth - gpsDrawAreaPadding * 2) / (gpsOverlayHeight - gpsDrawAreaPadding * 2);
        double scale;
        if (gpsDrawAreaAspectRatio > gpsAspectRatio)
        {
            //points cover the full height
            scale = gpsOverlayHeight - gpsDrawAreaPadding * 2;
        }
        else
        {
            //points cover the full width
            scale = gpsOverlayWidth - gpsDrawAreaPadding * 2;
        }
        //transform points into actual draw points
        List<SKPoint?> drawPoints = [];
        foreach ((double x, double y)? point in normalizedPoints)
            if (point is null)
                drawPoints.Add(null);
            else
            {
                float x = (float)((point?.x ?? 0) * scale + gpsDrawAreaPadding);
                float y = (float)((point?.y ?? 0) * scale + gpsDrawAreaPadding);
                drawPoints.Add(new SKPoint(x, y));
            }
        //create base gps overlay
        PathRendererOptions pathRendererOptions = new()
        {
            BitmapWidth = gpsOverlayWidth,
            BitmapHeight = gpsOverlayHeight,
            PrimaryColor = settings.GpsOutlineColor,
            SecondaryColor = settings.SecondaryColor,
            StrokeWidth = settings.GpsLineWidth,
            FadePointCount = (int)(settings.GpsFadeDurationSeconds * settings.FPS)
        };
        SKBitmap gpsBaseBitmap = PathRenderer.RenderFull(pathRendererOptions, drawPoints);
        pathRendererOptions.PrimaryColor = settings.PrimaryColor;
        DataFieldRendererOptions dataFieldRendererOptions = new()
        {
            BitmapHeight = dataOverlayHeight,
            BitmapWidth = dataOverlayWidth,
            LabelColor = settings.PrimaryColor,
            LabelFont = new SKFont(SKTypeface.FromFamilyName(settings.LabelFontFamily), settings.FontSizeSmall),
            ValueColor = settings.PrimaryColor,
            ValueFont = new SKFont(SKTypeface.FromFamilyName(settings.ValueFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), settings.FontSizeBig),
            UnitColor = settings.SecondaryColor,
            UnitFont = new SKFont(SKTypeface.FromFamilyName(settings.UnitFontFamily, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), settings.FontSizeSmall),
            LineSpacing = settings.LineSpacing
        };
        for (int i = 0; i < records.Count; ++i)
        {
            //create underlying bitmap
            SKBitmap sKBitmap = new(settings.Size.Width, settings.Size.Height);
            SKCanvas sKCanvas = new(sKBitmap);
            sKCanvas.Clear(settings.Background);
            //apply base gps overlay
            sKCanvas.DrawBitmap(gpsBaseBitmap, settings.Size.Width - settings.GPSOverlayWidth, 0, SKSamplingOptions.Default);
            //create partial gps path and apply over base gps overlay
            SKBitmap gpsPathOverlay = PathRenderer.RenderUntilPoint(pathRendererOptions, drawPoints, i);
            sKCanvas.DrawBitmap(gpsPathOverlay, settings.Size.Width - settings.GPSOverlayWidth, 0, SKSamplingOptions.Default);
            //create data field overlays and apply them in the correct place
            int row = 0, col = 0;
            //pace
            string paceString = CreatePaceStringFromSpeed(records[i].Speed);
            SKBitmap paceBitmap = DataFieldRenderer.Render(dataFieldRendererOptions, settings.PaceLabel, paceString, settings.PaceUnit);
            sKCanvas.DrawBitmap(paceBitmap, dataOverlayWidth * col, dataOverlayHeight * row, SKSamplingOptions.Default);
            if(++row >= dataFieldsPerColumn)
            {
                row = 0;
                ++col;
            }
            //heart rate
            dataFieldRendererOptions.ValueColor = settings.ZoneBrushes[GetHeartRateZone(records[i].HeartRate ?? 0, settings)];
            SKBitmap heartRateBitmap = DataFieldRenderer.Render(dataFieldRendererOptions, settings.HrLabel, records[i].HeartRate.ToString() ?? string.Empty, settings.HrUnit);
            sKCanvas.DrawBitmap(heartRateBitmap, dataOverlayWidth * col, dataOverlayHeight * row, SKSamplingOptions.Default);
            if (++row >= dataFieldsPerColumn)
            {
                row = 0;
                ++col;
            }
            dataFieldRendererOptions.ValueColor = settings.PrimaryColor;
            //distance
            SKBitmap distanceBitmap = DataFieldRenderer.Render(dataFieldRendererOptions, settings.DistanceLabel, (records[i].Distance / 1000)?.ToString("0.00") ?? string.Empty, settings.DistanceUnit);
            sKCanvas.DrawBitmap(distanceBitmap, dataOverlayWidth * col, dataOverlayHeight * row, SKSamplingOptions.Default);
            if (++row >= dataFieldsPerColumn)
            {
                row = 0;
                ++col;
            }
            //timestamp
            SKBitmap timestampBitmap = DataFieldRenderer.Render(dataFieldRendererOptions, "Timestamp", records[i].TimeStamp.ToShortTimeString(), string.Empty);
            sKCanvas.DrawBitmap(timestampBitmap, dataOverlayWidth * col, dataOverlayHeight * row, SKSamplingOptions.Default);
            //create frame and return
            yield return new SKBitmapFrame(sKBitmap);
        }
    }

    private static int GetHeartRateZone(int heartRate, OverlaySettings settings)
    {
        float percentLthr = (float)heartRate / settings.LTHR;
        for (int i = 0; i < settings.ZoneMaxPercent.Length; ++i)
            if (percentLthr <= settings.ZoneMaxPercent[i])
                return i;
        return 0;
    }

    private static string CreatePaceStringFromSpeed(float? speed)
    {
        int secPerKm = (int)(1000 / (speed ?? 0f));
        string paceString;
        if (secPerKm < 1 || secPerKm > 3600)
            paceString = "--'--\"";
        else
            paceString = $"{secPerKm / 60}'{secPerKm % 60:D2}\"";
        return paceString;
    }

    /// <summary>
    /// Excludes last original record from output list
    /// </summary>
    /// <param name="originalList"></param>
    /// <param name="fps">Determines the number of inserted records.
    ///                   Normally the number of output records for one record will be equal to fps value,
    ///                   but in case the time gap between two consecutive records is greater than 1 second the number 
    ///                   of output records between those two records will be multiplied by the time gap</param>
    /// <returns></returns>
    private static List<IActivityRecord> InsertInterpolatedRecord(List<IActivityRecord> originalList, uint fps)
    {
        List<IActivityRecord> newList = [];
        for (int i = 0; i < originalList.Count - 1; ++i)
        {
            int secondsBetweenRecords = (originalList[i + 1].TimeStamp - originalList[i].TimeStamp).Seconds;
            int interpolatedRecordCount = (int)fps * secondsBetweenRecords;
            //define interpolation step values
            double timeStampStep = secondsBetweenRecords / (double)interpolatedRecordCount;
            float heartRateStep = (float)((originalList[i + 1].HeartRate - originalList[i].HeartRate) ?? 0) / interpolatedRecordCount;
            float speedStep = ((originalList[i + 1].Speed - originalList[i].Speed) ?? 0f) / interpolatedRecordCount;
            float distanceStep = ((originalList[i + 1].Distance - originalList[i].Distance) ?? 0f) / interpolatedRecordCount;
            double gpsLatitudeStep = ((originalList[i + 1].GPSPoint?.Latitude - originalList[i].GPSPoint?.Latitude) ?? 0d) / interpolatedRecordCount;
            double gpsLongitudeStep = ((originalList[i + 1].GPSPoint?.Longitude - originalList[i].GPSPoint?.Longitude) ?? 0d) / interpolatedRecordCount;
            //create the records
            for (int j = 0; j < interpolatedRecordCount; ++j)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                GpsPoint? newPoint;
                double newRecordLatitude, newRecordLongitude;
                if (originalList[i].GPSPoint != null)
                {
                    newRecordLatitude = originalList[i].GPSPoint.Latitude + gpsLatitudeStep * j;
                    newRecordLongitude = originalList[i].GPSPoint.Longitude + gpsLongitudeStep * j;
                    newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                }
                else if (originalList[i + 1].GPSPoint != null)
                {
                    newRecordLatitude = originalList[i + 1].GPSPoint.Latitude;
                    newRecordLongitude = originalList[i + 1].GPSPoint.Longitude;
                    newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                }
                else
                    newPoint = null;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                newList.Add(new FitRecord()
                {
                    TimeStamp = originalList[i].TimeStamp.AddSeconds(timeStampStep * j),
                    HeartRate = (int)(originalList[i].HeartRate ?? 0 + heartRateStep * j),
                    Speed = (originalList[i].Speed ?? 0f) + speedStep * j,
                    Distance = (originalList[i].Distance ?? 0f) + distanceStep * j,
                    GPSPoint = newPoint
                });
            }
        }
        return newList;
    }

    private static List<(double x, double y)?> ProcessGpsPoints(List<IActivityRecord> records, out double gpsAspectRatio)
    {
        List<GpsPoint?> points = [];
        foreach (IActivityRecord record in records)
            points.Add(record.GPSPoint);
        return GpsPoint.PointsListToUnitaryScreenSpace(points, out gpsAspectRatio);
    }
}
