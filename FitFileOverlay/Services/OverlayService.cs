using FFMpegCore;
using FFMpegCore.Extensions.SkiaSharp;
using FFMpegCore.Pipes;
using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;
using System.IO;

namespace FitFileOverlay.Services;

public partial class OverlayService : ObservableObject, IOverlayService
{
    public event Action? NewFileLoaded;
    public event NewSettingsAppiedEventHandler? NewSettingsApplied;

    [ObservableProperty]
    public partial OverlaySettings? Settings { get; set; }
    [ObservableProperty]
    public partial FitFile? File { get; private set; }

    public bool Load(string fileName)
    {
        FitFile newFile = new(fileName);
        if (!newFile.IsValid)
            return false;
        File = newFile;
        return true;
    }

    public async Task Export(string outputFilename, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null)
    {
        if (File != null && File.IsValid && Settings != null)
        {
            //insert interpolated records if needed
            List<IActivityRecord> fullRecordList;
            if (Settings.FPS > 1)
                fullRecordList = InterpolateRecords(File.Records, Settings.FPS);
            else
                fullRecordList = File.Records;
            //create list of unitary screenspace gps points
            List<(double x, double y)?> normalizedPoints = ProcessGpsPoints(fullRecordList, out double gpsAspectRatio);
            //Generate video frames and encode video using FFMpegCore
            IEnumerable<IVideoFrame> frames = CreateVideoFrames(fullRecordList, normalizedPoints, gpsAspectRatio, progressReportCallback);
            RawVideoPipeSource framesSource = new(frames){ FrameRate = Settings.FPS };
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilename) ?? string.Empty);
            await FFMpegArguments.FromPipeInput(framesSource)
                .OutputToFile(outputFilename, true, opt => opt
                    .WithFramerate(Settings.FPS)
                    .WithVideoCodec("prores_ks")
                    .ForcePixelFormat("yuva444p10le")
                    .WithCustomArgument("-profile:v 4444")
                    .WithConstantRateFactor(17))
                .CancellableThrough(cancellationToken ?? new CancellationToken())
                .ProcessAsynchronously(throwOnError: true);
        }
    }

    public SKBitmap? GetSnapshot(double activityPercent)
    {
        if (File != null && File.IsValid && Settings != null)
        {
            int recordIndex = (int)(activityPercent * File.Records.Count);
            if (recordIndex < 0) recordIndex = 0;
            if (recordIndex >= File.Records.Count) recordIndex = File.Records.Count - 1;
            return GetSnapshotAtRecord(recordIndex);
        }
        return null;
    }

    partial void OnSettingsChanged(OverlaySettings? oldValue, OverlaySettings? newValue)
    {
        NewSettingsApplied?.Invoke(oldValue, newValue);
    }

    partial void OnFileChanged(FitFile? value)
    {
        NewFileLoaded?.Invoke();
    }

    private SKBitmap? GetSnapshotAtRecord(int recordIndex)
    {
        if(File == null || !File.IsValid || Settings == null)
            return null;    
        if (recordIndex < 0 || recordIndex >= File.Records.Count)
            return null;
        if (!Settings.IsGpsOverlayEnabled && !Settings.IsDataFieldsOverlayEnabled)
            return null;
        //create list of unitary screenspace gps points
        List<(double x, double y)?> normalizedPoints = ProcessGpsPoints(File.Records, out double gpsAspectRatio);
        //define layout
        PathRendererOptions pathRendererOptions = CreatePathRendererOptionsFromSettings(Settings);
        pathRendererOptions.FadePointCount = Settings.GpsFadeDurationSeconds;
        float gpsDrawAreaPadding = Settings.GpsLineWidth * 2;//add some padding so the points on the border dont get cut off
        double gpsDrawAreaAspectRatio = (double)(pathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2) / (pathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2);
        double scale;
        if (gpsDrawAreaAspectRatio > gpsAspectRatio)
        {
            //points cover the full height
            scale = pathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2;
        }
        else
        {
            //points cover the full width
            scale = pathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2;
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
        //create underlying bitmap
        int overlayWidth = (Settings.IsDataFieldsOverlayEnabled ? Settings.DataFieldsOverlayWidth : 0)
            + (Settings.IsGpsOverlayEnabled ? Settings.GpsOverlayWidth : 0);
        SKBitmap sKBitmap = new(overlayWidth, Settings.OverlayHeight);
        SKCanvas sKCanvas = new(sKBitmap);
        sKCanvas.Clear(Settings.Background);
        if (Settings.IsDataFieldsOverlayEnabled)
        {
            //create data fields overlay and apply
            SKBitmap? dataFieldsOverlay = CreateDataFieldsOverlay(File.Records.ElementAt(recordIndex));
            if (dataFieldsOverlay != null && !dataFieldsOverlay.IsEmpty)
                sKCanvas.DrawBitmap(dataFieldsOverlay, 0, 0, SKSamplingOptions.Default);
        }
        if (Settings.IsGpsOverlayEnabled)
        {
            //create base gps overlay
            SKBitmap gpsBaseBitmap = PathRenderer.RenderFull(pathRendererOptions, drawPoints);
            pathRendererOptions.PrimaryColor = Settings.PrimaryColor;
            SKBitmap? pathCacheBitmap = null;
            //apply base gps overlay
            sKCanvas.DrawBitmap(gpsBaseBitmap, overlayWidth - Settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
            //create partial gps path and apply over base gps overlay
            SKBitmap gpsPathOverlay = PathRenderer.RenderUntilPoint(pathRendererOptions, drawPoints, recordIndex, ref pathCacheBitmap);
            sKCanvas.DrawBitmap(gpsPathOverlay, overlayWidth - Settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
        }
        return sKBitmap;
    }

    private IEnumerable<IVideoFrame> CreateVideoFrames(
        List<IActivityRecord> records,
        List<(double x, double y)?> normalizedPoints,
        double gpsAspectRatio,
        Action<double>? progressReportCallback = null)
    {
        //define layout
        int overlayWidth = (Settings!.IsDataFieldsOverlayEnabled ? Settings.DataFieldsOverlayWidth : 0)
            + (Settings!.IsGpsOverlayEnabled ? Settings.GpsOverlayWidth : 0);
        PathRendererOptions pathRendererOptions = CreatePathRendererOptionsFromSettings(Settings!); ;
        List<SKPoint?> drawPoints = [];
        SKBitmap? gpsBaseBitmap = null;
        if (Settings!.IsGpsOverlayEnabled)
        {
            float gpsDrawAreaPadding = Settings.GpsLineWidth * 2;//add some padding so the points on the border dont get cut off
            double gpsDrawAreaAspectRatio = (double)(pathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2) / (pathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2);
            double scale;
            if (gpsDrawAreaAspectRatio > gpsAspectRatio)
            {
                //points cover the full height
                scale = pathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2;
            }
            else
            {
                //points cover the full width
                scale = pathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2;
            }
            //transform points into actual draw points
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
            gpsBaseBitmap = PathRenderer.RenderFull(pathRendererOptions, drawPoints);
            pathRendererOptions.PrimaryColor = Settings.PrimaryColor;
        }
        SKBitmap? pathCacheBitmap = null;
        for (int i = 0; i < records.Count; ++i)
        {
            //create underlying bitmap
            SKBitmap sKBitmap = new(overlayWidth, Settings.OverlayHeight);
            SKCanvas sKCanvas = new(sKBitmap);
            sKCanvas.Clear(Settings.Background);
            if (Settings.IsDataFieldsOverlayEnabled)
            {
                //create data fields overlay and apply
                SKBitmap? dataFieldsOverlay = CreateDataFieldsOverlay(records[i]);
                if (dataFieldsOverlay != null && !dataFieldsOverlay.IsEmpty)
                    sKCanvas.DrawBitmap(dataFieldsOverlay, 0, 0, SKSamplingOptions.Default);
            }
            if (Settings.IsGpsOverlayEnabled)
            {
                //apply base gps overlay
                sKCanvas.DrawBitmap(gpsBaseBitmap, overlayWidth - Settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
                //create partial gps path and apply over base gps overlay
                SKBitmap gpsPathOverlay = PathRenderer.RenderUntilPoint(pathRendererOptions, drawPoints, i, ref pathCacheBitmap);
                sKCanvas.DrawBitmap(gpsPathOverlay, overlayWidth - Settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
            }
            //create frame and return
            progressReportCallback?.Invoke((double)i / records.Count);
            yield return new BitmapVideoFrameWrapper(sKBitmap);
        }
    }

    private SKBitmap? CreateDataFieldsOverlay(IActivityRecord record)
    {
        DataFieldRendererOptions rendererOptions = CreateDataFieldRendererOptionsFromSettings(Settings!);
        int dataFieldCount = Settings!.DrawnDataFields.Count;
        int dataFieldsPerColumn = (int)Math.Ceiling((double)dataFieldCount / Settings.DataOverlayColumnCount);
        int bitmapWidth = Settings.DataFieldsOverlayWidth;
        if (bitmapWidth <= 0)
            return null;
        SKBitmap sKBitmap = new(bitmapWidth, Settings.OverlayHeight);
        SKCanvas sKCanvas = new(sKBitmap);
        //create data field overlays and apply them in the correct place
        int row = 0, col = 0;
        foreach (DataFieldType dataField in Settings.DrawnDataFields)
        {
            SKBitmap? dataFieldBitmap = CreateDataFieldBitmap(record, rendererOptions, dataField);
            if (dataFieldBitmap != null)
                sKCanvas.DrawBitmap(dataFieldBitmap, rendererOptions.BitmapWidth * col, rendererOptions.BitmapHeight * row, SKSamplingOptions.Default);
            if (++row >= dataFieldsPerColumn)
            {
                row = 0;
                ++col;
            }
        }
        return sKBitmap;
    }

    private SKBitmap? CreateDataFieldBitmap(IActivityRecord record, DataFieldRendererOptions rendererOptionsBase, DataFieldType dataField)
    {
        string label, value, unit;
        switch (dataField)
        {
            case DataFieldType.Pace:
                label = Settings!.PaceLabel;
                value = CreatePaceStringFromSpeed(record.Speed);
                unit = Settings!.PaceUnit;
                break;
            case DataFieldType.HeartRate:
                label = Settings!.HrLabel;
                value = record.HeartRate.ToString() ?? string.Empty;
                unit = Settings!.HrUnit;
                rendererOptionsBase.ValueColor = GetHeartRateZoneBrush(record.HeartRate ?? 0);
                break;
            case DataFieldType.Distance:
                label = Settings!.DistanceLabel;
                //meters to kilometers
                value = (record.Distance / 1000)?.ToString("0.00") ?? string.Empty;
                unit = Settings!.DistanceUnit;
                break;
            case DataFieldType.Cadence:
                label = Settings!.CadenceLabel;
                //half cadence (rpm) to full cadence (spm)
                value = (record.Cadence * 2)?.ToString("0") ?? string.Empty;
                unit = Settings!.CadenceUnit;
                break;
            case DataFieldType.Speed:
                label = Settings!.SpeedLabel;
                // m/s to km/h
                value = (record.Speed * 3.6)?.ToString("0.0") ?? string.Empty;
                unit = Settings!.SpeedUnit;
                break;
            case DataFieldType.Power:
                label = Settings!.PowerLabel;
                value = record.Power?.ToString() ?? string.Empty;
                unit = Settings!.PowerUnit;
                break;
            case DataFieldType.StrideLength:
                label = Settings!.StrideLengthLabel;
                // milimeters to meters
                value = (record.StrideLength / 1000)?.ToString("0.00") ?? string.Empty;
                unit = Settings!.StrideLengthUnit;
                break;
            case DataFieldType.Timestamp:
                label = string.Empty;
                value = record.TimeStamp.ToLocalTime().ToString("dd-MMM-yy H:mm:ss");
                unit = string.Empty;
                rendererOptionsBase.ValueFont =
                    new SKFont(SKTypeface.FromFamilyName(
                            familyName: Settings!.ValueFontFamily,
                            weight: Settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                            width: SKFontStyleWidth.Normal,
                            slant: Settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                        Settings.TimestampFontSize);
                break;
            default:
                return null;
        }
        return DataFieldRenderer.Render(rendererOptionsBase, label, value, unit);
    }

    private SKColor GetHeartRateZoneBrush(int heartRate)
    {
        int lthr = File!.LactateThresholdHeartRate ?? Settings!.LTHR;
        if (Settings!.UseCustomLTHR)
            lthr = Settings.LTHR;
        float percentLthr = (float)heartRate / lthr;
        int zoneIndex = 0;
        for (int i = 0; i < Settings.ZoneMaxPercent.Length; ++i)
            if (percentLthr <= Settings.ZoneMaxPercent[i])
            {
                zoneIndex = i;
                break;
            }
        return zoneIndex switch
        {
            0 => Settings.Zone1Brush,
            1 => Settings.Zone2Brush,
            2 => Settings.Zone3Brush,
            3 => Settings.Zone4Brush,
            4 => Settings.Zone5Brush,
            _ => SKColors.Transparent,
        };
    }

    #region STATIC METHODS
    /// <summary>
    /// Excludes last original record from output list
    /// </summary>
    /// <param name="originalList"></param>
    /// <param name="fps">Determines the number of inserted records.
    ///                   Normally the number of output records for one record will be equal to fps value,
    ///                   but in case the time gap between two consecutive records is greater than 1 second the number 
    ///                   of output records between those two records will be multiplied by the time gap</param>
    /// <returns></returns>
    private static List<IActivityRecord> InterpolateRecords(List<IActivityRecord> originalList, uint fps)
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
            float cadenceStep = ((originalList[i + 1].Cadence - originalList[i].Cadence) ?? 0f) / interpolatedRecordCount;
            float powerStep = (float)((originalList[i + 1].Power - originalList[i].Power) ?? 0) / interpolatedRecordCount;
            float strideLengthStep = ((originalList[i + 1].StrideLength - originalList[i].StrideLength) ?? 0f) / interpolatedRecordCount;
            double gpsLatitudeStep = ((originalList[i + 1].GPSPoint?.Latitude - originalList[i].GPSPoint?.Latitude) ?? 0d) / interpolatedRecordCount;
            double gpsLongitudeStep = ((originalList[i + 1].GPSPoint?.Longitude - originalList[i].GPSPoint?.Longitude) ?? 0d) / interpolatedRecordCount;
            //create the records
            for (int j = 0; j < interpolatedRecordCount; ++j)
            {
                GpsPoint? newPoint;
                double newRecordLatitude, newRecordLongitude;
                if (originalList[i].GPSPoint != null)
                {
                    newRecordLatitude = originalList[i].GPSPoint!.Latitude + gpsLatitudeStep * j;
                    newRecordLongitude = originalList[i].GPSPoint!.Longitude + gpsLongitudeStep * j;
                    newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                }
                else if (originalList[i + 1].GPSPoint != null)
                {
                    newRecordLatitude = originalList[i + 1].GPSPoint!.Latitude;
                    newRecordLongitude = originalList[i + 1].GPSPoint!.Longitude;
                    newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                }
                else
                    newPoint = null;
                newList.Add(new FitRecord()
                {
                    TimeStamp = originalList[i].TimeStamp.AddSeconds(timeStampStep * j),
                    HeartRate = (int)((originalList[i].HeartRate ?? 0) + heartRateStep * j),
                    Speed = (originalList[i].Speed ?? 0f) + speedStep * j,
                    Distance = (originalList[i].Distance ?? 0f) + distanceStep * j,
                    Cadence = (originalList[i].Cadence ?? 0) + cadenceStep * j,
                    Power = (int)((originalList[i].Power ?? 0) + powerStep * j),
                    StrideLength = (originalList[i].StrideLength ?? 0f) + strideLengthStep * j,
                    GPSPoint = newPoint
                });
            }
        }
        return newList;
    }

    private static DataFieldRendererOptions CreateDataFieldRendererOptionsFromSettings(OverlaySettings settings)
    {
        int dataOverlayWidth = settings.DataFieldsOverlayWidth / settings.DataOverlayColumnCount;
        int dataOverlayHeight = (int)(settings.ValueFontSize + settings.LabelFontSize + settings.LineSpacing + settings.DataOverlayVerticalSpacing);
        DataFieldRendererOptions dataFieldRendererOptions = new()
        {
            BitmapHeight = dataOverlayHeight,
            BitmapWidth = dataOverlayWidth,
            LabelColor = settings.PrimaryColor,
            LabelFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.LabelFontFamily,
                    weight: settings.IsLabelFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsLabelFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.LabelFontSize),
            ValueColor = settings.PrimaryColor,
            ValueFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.ValueFontFamily,
                    weight: settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.ValueFontSize),
            UnitColor = settings.SecondaryColor,
            UnitFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.UnitFontFamily,
                    weight: settings.IsUnitFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsUnitFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.UnitFontSize),
            LineSpacing = settings.LineSpacing
        };
        return dataFieldRendererOptions;
    }

    private static PathRendererOptions CreatePathRendererOptionsFromSettings(OverlaySettings settings)
    {
        int gpsOverlayWidth = settings.GpsOverlayWidth;
        int gpsOverlayHeight = settings.OverlayHeight;
        PathRendererOptions pathRendererOptions = new()
        {
            BitmapWidth = gpsOverlayWidth,
            BitmapHeight = gpsOverlayHeight,
            PrimaryColor = settings.GpsOutlineColor,
            SecondaryColor = settings.SecondaryColor,
            StrokeWidth = settings.GpsLineWidth,
            FadePointCount = (int)(settings.GpsFadeDurationSeconds * settings.FPS)
        };
        return pathRendererOptions;
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

    private static List<(double x, double y)?> ProcessGpsPoints(ICollection<IActivityRecord> records, out double gpsAspectRatio)
    {
        List<GpsPoint?> points = [];
        foreach (IActivityRecord record in records)
            points.Add(record.GPSPoint);
        return GpsPoint.PointsListToUnitaryScreenSpace(points, out gpsAspectRatio);
    }
    #endregion
}
