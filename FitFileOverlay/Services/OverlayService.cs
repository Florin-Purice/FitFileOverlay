using FFMpegCore;
using FFMpegCore.Extensions.SkiaSharp;
using FFMpegCore.Pipes;
using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Services;

public partial class OverlayService(OverlaySettings overlaySettings) : ObservableObject, IOverlayService
{
    public event Action? NewFileLoaded;
    public event NewSettingsAppiedEventHandler? NewSettingsApplied;

    [ObservableProperty]
    public partial OverlaySettings? Settings { get; set; } = overlaySettings;
    [ObservableProperty]
    public partial FitFile? File { get; private set; }

    public bool Load(string fileName)
    {
        try
        {
            FitFile newFile = new(fileName);
            if(!newFile.IsValid)
                return false;
            File = newFile;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task Export(string outputPath, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null)
    {
        if (File != null && File.IsValid)
            await OverlayProcessor.ExportVideo(File.Records, outputPath, Settings ?? new OverlaySettings(), GetLTHR(), progressReportCallback, cancellationToken);
    }

    public SKBitmap? GetSnapshot(double activityPercent)
    {
        if (File != null && File.IsValid)
            return OverlayProcessor.GetSnapshotAtActivityPercent(File.Records, Settings ?? new OverlaySettings(), GetLTHR(), activityPercent);
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

    private int GetLTHR()
    {
        if (File != null && File.IsValid && Settings != null)
        {
            int lthr = File.LactateThresholdHeartRate ?? Settings.LTHR;
            if (Settings.UseCustomLTHR)
                lthr = Settings.LTHR;
            return lthr;
        }
        return Settings?.LTHR ?? 145;
    }

    private static class OverlayProcessor
    {
        public static async Task ExportVideo(
            ICollection<IActivityRecord> records,
            string outFileName,
            OverlaySettings settings,
            int lthr,
            Action<double>? progressReportCallback = null,
            CancellationToken? cancellationToken = null)
        {
            //insert interpolated records if needed
            List<IActivityRecord> fullRecordList;
            if (settings.FPS > 1)
                fullRecordList = InterpolateRecords(records, settings.FPS);
            else
                fullRecordList = [.. records];
            //create list of unitary screenspace gps points
            List<(double x, double y)?> normalizedPoints = ProcessGpsPoints(fullRecordList, out double gpsAspectRatio);
            //Generate video frames and encode video using FFMpegCore
            IEnumerable<IVideoFrame> frames = CreateVideoFrames(fullRecordList, normalizedPoints, gpsAspectRatio, settings, lthr, progressReportCallback);
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

        public static SKBitmap? GetSnapshotAtRecord(ICollection<IActivityRecord> records, OverlaySettings settings, int lthr, int recordIndex)
        {
            if (recordIndex < 0 || recordIndex >= records.Count)
                return null;
            if (!settings.IsGpsOverlayEnabled && !settings.IsDataFieldsOverlayEnabled)
                return null;
            //create list of unitary screenspace gps points
            List<(double x, double y)?> normalizedPoints = ProcessGpsPoints(records, out double gpsAspectRatio);
            //define layout
            PathRendererOptions pathRendererOptions = CreatePathRendererOptionsFromSettings(settings);
            pathRendererOptions.FadePointCount = settings.GpsFadeDurationSeconds;
            float gpsDrawAreaPadding = settings.GpsLineWidth * 2;//add some padding so the points on the border dont get cut off
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
            int overlayWidth = (settings.IsDataFieldsOverlayEnabled ? settings.DataFieldsOverlayWidth : 0)
                + (settings.IsGpsOverlayEnabled ? settings.GpsOverlayWidth : 0);
            SKBitmap sKBitmap = new(overlayWidth, settings.OverlayHeight);
            SKCanvas sKCanvas = new(sKBitmap);
            sKCanvas.Clear(settings.Background);
            if (settings.IsDataFieldsOverlayEnabled)
            {
                //create data fields overlay and apply
                SKBitmap? dataFieldsOverlay = CreateDataFieldsOverlay(records.ElementAt(recordIndex), settings, lthr);
                if (dataFieldsOverlay != null && !dataFieldsOverlay.IsEmpty)
                    sKCanvas.DrawBitmap(dataFieldsOverlay, 0, 0, SKSamplingOptions.Default);
            }
            if (settings.IsGpsOverlayEnabled)
            {
                //create base gps overlay
                SKBitmap gpsBaseBitmap = PathRenderer.RenderFull(pathRendererOptions, drawPoints);
                pathRendererOptions.PrimaryColor = settings.PrimaryColor;
                SKBitmap? pathCacheBitmap = null;
                //apply base gps overlay
                sKCanvas.DrawBitmap(gpsBaseBitmap, overlayWidth - settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
                //create partial gps path and apply over base gps overlay
                SKBitmap gpsPathOverlay = PathRenderer.RenderUntilPoint(pathRendererOptions, drawPoints, recordIndex, ref pathCacheBitmap);
                sKCanvas.DrawBitmap(gpsPathOverlay, overlayWidth - settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
            }
            return sKBitmap;
        }

        /// <summary>
        /// </summary>
        /// <param name="activityPercent">Value between 0 and 1</param>
        /// <returns></returns>
        public static SKBitmap? GetSnapshotAtActivityPercent(ICollection<IActivityRecord> records, OverlaySettings settings, int lthr, double activityPercent)
        {
            int recordIndex = (int)(activityPercent * records.Count);
            if (recordIndex < 0) recordIndex = 0;
            if (recordIndex >= records.Count) recordIndex = records.Count - 1;
            return GetSnapshotAtRecord(records, settings, lthr, recordIndex);
        }

        private static IEnumerable<IVideoFrame> CreateVideoFrames(
            List<IActivityRecord> records,
            List<(double x, double y)?> normalizedPoints,
            double gpsAspectRatio,
            OverlaySettings settings,
            int lthr,
            Action<double>? progressReportCallback = null)
        {
            //define layout
            int overlayWidth = (settings.IsDataFieldsOverlayEnabled ? settings.DataFieldsOverlayWidth : 0)
                + (settings.IsGpsOverlayEnabled ? settings.GpsOverlayWidth : 0);
            PathRendererOptions pathRendererOptions = CreatePathRendererOptionsFromSettings(settings); ;
            List<SKPoint?> drawPoints = [];
            SKBitmap? gpsBaseBitmap = null;
            if (settings.IsGpsOverlayEnabled)
            {
                float gpsDrawAreaPadding = settings.GpsLineWidth * 2;//add some padding so the points on the border dont get cut off
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
                pathRendererOptions.PrimaryColor = settings.PrimaryColor;
            }
            SKBitmap? pathCacheBitmap = null;
            for (int i = 0; i < records.Count; ++i)
            {
                //create underlying bitmap
                SKBitmap sKBitmap = new(overlayWidth, settings.OverlayHeight);
                SKCanvas sKCanvas = new(sKBitmap);
                sKCanvas.Clear(settings.Background);
                if (settings.IsDataFieldsOverlayEnabled)
                {
                    //create data fields overlay and apply
                    SKBitmap? dataFieldsOverlay = CreateDataFieldsOverlay(records[i], settings, lthr);
                    if (dataFieldsOverlay != null && !dataFieldsOverlay.IsEmpty)
                        sKCanvas.DrawBitmap(dataFieldsOverlay, 0, 0, SKSamplingOptions.Default);
                }
                if (settings.IsGpsOverlayEnabled)
                {
                    //apply base gps overlay
                    sKCanvas.DrawBitmap(gpsBaseBitmap, overlayWidth - settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
                    //create partial gps path and apply over base gps overlay
                    SKBitmap gpsPathOverlay = PathRenderer.RenderUntilPoint(pathRendererOptions, drawPoints, i, ref pathCacheBitmap);
                    sKCanvas.DrawBitmap(gpsPathOverlay, overlayWidth - settings.GpsOverlayWidth, 0, SKSamplingOptions.Default);
                }
                //create frame and return
                progressReportCallback?.Invoke((double)i / records.Count);
                yield return new BitmapVideoFrameWrapper(sKBitmap);
            }
        }

        private static SKBitmap? CreateDataFieldsOverlay(IActivityRecord record, OverlaySettings settings, int lthr)
        {
            DataFieldRendererOptions rendererOptions = CreateDataFieldRendererOptionsFromSettings(settings);
            int dataFieldCount = settings.DrawnDataFields.Count;
            int dataFieldsPerColumn = (int)Math.Ceiling((double)dataFieldCount / settings.DataOverlayColumnCount);
            int bitmapWidth = settings.DataFieldsOverlayWidth;
            if (bitmapWidth <= 0)
                return null;
            SKBitmap sKBitmap = new(bitmapWidth, settings.OverlayHeight);
            SKCanvas sKCanvas = new(sKBitmap);
            //create data field overlays and apply them in the correct place
            int row = 0, col = 0;
            foreach (DataFieldType dataField in settings.DrawnDataFields)
            {
                SKBitmap? dataFieldBitmap = CreateDataFieldBitmap(record, settings, lthr, rendererOptions, dataField);
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

        private static SKBitmap? CreateDataFieldBitmap(IActivityRecord record, OverlaySettings settings, int lthr, DataFieldRendererOptions rendererOptionsBase, DataFieldType dataField)
        {
            string label, value, unit;
            switch (dataField)
            {
                case DataFieldType.Pace:
                    label = settings.PaceLabel;
                    value = CreatePaceStringFromSpeed(record.Speed);
                    unit = settings.PaceUnit;
                    break;
                case DataFieldType.HeartRate:
                    label = settings.HrLabel;
                    value = record.HeartRate.ToString() ?? string.Empty;
                    unit = settings.HrUnit;
                    rendererOptionsBase.ValueColor = GetHeartRateZoneBrush(record.HeartRate ?? 0, lthr, settings);
                    break;
                case DataFieldType.Distance:
                    label = settings.DistanceLabel;
                    //meters to kilometers
                    value = (record.Distance / 1000)?.ToString("0.00") ?? string.Empty;
                    unit = settings.DistanceUnit;
                    break;
                case DataFieldType.Cadence:
                    label = settings.CadenceLabel;
                    //half cadence (rpm) to full cadence (spm)
                    value = (record.Cadence * 2)?.ToString("0") ?? string.Empty;
                    unit = settings.CadenceUnit;
                    break;
                case DataFieldType.Speed:
                    label = settings.SpeedLabel;
                    // m/s to km/h
                    value = (record.Speed * 3.6)?.ToString("0.0") ?? string.Empty;
                    unit = settings.SpeedUnit;
                    break;
                case DataFieldType.Power:
                    label = settings.PowerLabel;
                    value = record.Power?.ToString() ?? string.Empty;
                    unit = settings.PowerUnit;
                    break;
                case DataFieldType.StrideLength:
                    label = settings.StrideLengthLabel;
                    // milimeters to meters
                    value = (record.StrideLength / 1000)?.ToString("0.00") ?? string.Empty;
                    unit = settings.StrideLengthUnit;
                    break;
                case DataFieldType.Timestamp:
                    label = string.Empty;
                    value = record.TimeStamp.ToLocalTime().ToString("dd-MMM-yy H:mm:ss");
                    unit = string.Empty;
                    rendererOptionsBase.ValueFont =
                        new SKFont(SKTypeface.FromFamilyName(
                                familyName: settings.ValueFontFamily,
                                weight: settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                                width: SKFontStyleWidth.Normal,
                                slant: settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                            settings.TimestampFontSize);
                    break;
                default:
                    return null;
            }
            return DataFieldRenderer.Render(rendererOptionsBase, label, value, unit);
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

        private static SKColor GetHeartRateZoneBrush(int heartRate, int lthr, OverlaySettings settings)
        {
            float percentLthr = (float)heartRate / lthr;
            int zoneIndex = 0;
            for (int i = 0; i < settings.ZoneMaxPercent.Length; ++i)
                if (percentLthr <= settings.ZoneMaxPercent[i])
                {
                    zoneIndex = i;
                    break;
                }
            return zoneIndex switch
            {
                0 => settings.Zone1Brush,
                1 => settings.Zone2Brush,
                2 => settings.Zone3Brush,
                3 => settings.Zone4Brush,
                4 => settings.Zone5Brush,
                _ => SKColors.Transparent,
            };
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
        private static List<IActivityRecord> InterpolateRecords(ICollection<IActivityRecord> originalList, uint fps)
        {
            List<IActivityRecord> newList = [];
            for (int i = 0; i < originalList.Count - 1; ++i)
            {
                int secondsBetweenRecords = (originalList.ElementAt(i + 1).TimeStamp - originalList.ElementAt(i).TimeStamp).Seconds;
                int interpolatedRecordCount = (int)fps * secondsBetweenRecords;
                //define interpolation step values
                double timeStampStep = secondsBetweenRecords / (double)interpolatedRecordCount;
                float heartRateStep = (float)((originalList.ElementAt(i + 1).HeartRate - originalList.ElementAt(i).HeartRate) ?? 0) / interpolatedRecordCount;
                float speedStep = ((originalList.ElementAt(i + 1).Speed - originalList.ElementAt(i).Speed) ?? 0f) / interpolatedRecordCount;
                float distanceStep = ((originalList.ElementAt(i + 1).Distance - originalList.ElementAt(i).Distance) ?? 0f) / interpolatedRecordCount;
                float cadenceStep = ((originalList.ElementAt(i + 1).Cadence - originalList.ElementAt(i).Cadence) ?? 0f) / interpolatedRecordCount;
                float powerStep = (float)((originalList.ElementAt(i + 1).Power - originalList.ElementAt(i).Power) ?? 0) / interpolatedRecordCount;
                float strideLengthStep = ((originalList.ElementAt(i + 1).StrideLength - originalList.ElementAt(i).StrideLength) ?? 0f) / interpolatedRecordCount;
                double gpsLatitudeStep = ((originalList.ElementAt(i + 1).GPSPoint?.Latitude - originalList.ElementAt(i).GPSPoint?.Latitude) ?? 0d) / interpolatedRecordCount;
                double gpsLongitudeStep = ((originalList.ElementAt(i + 1).GPSPoint?.Longitude - originalList.ElementAt(i).GPSPoint?.Longitude) ?? 0d) / interpolatedRecordCount;
                //create the records
                for (int j = 0; j < interpolatedRecordCount; ++j)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    GpsPoint? newPoint;
                    double newRecordLatitude, newRecordLongitude;
                    if (originalList.ElementAt(i).GPSPoint != null)
                    {
                        newRecordLatitude = originalList.ElementAt(i).GPSPoint.Latitude + gpsLatitudeStep * j;
                        newRecordLongitude = originalList.ElementAt(i).GPSPoint.Longitude + gpsLongitudeStep * j;
                        newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                    }
                    else if (originalList.ElementAt(i + 1).GPSPoint != null)
                    {
                        newRecordLatitude = originalList.ElementAt(i + 1).GPSPoint.Latitude;
                        newRecordLongitude = originalList.ElementAt(i + 1).GPSPoint.Longitude;
                        newPoint = new GpsPoint(newRecordLatitude, newRecordLongitude);
                    }
                    else
                        newPoint = null;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    newList.Add(new FitRecord()
                    {
                        TimeStamp = originalList.ElementAt(i).TimeStamp.AddSeconds(timeStampStep * j),
                        HeartRate = (int)((originalList.ElementAt(i).HeartRate ?? 0) + heartRateStep * j),
                        Speed = (originalList.ElementAt(i).Speed ?? 0f) + speedStep * j,
                        Distance = (originalList.ElementAt(i).Distance ?? 0f) + distanceStep * j,
                        Cadence = (originalList.ElementAt(i).Cadence ?? 0) + cadenceStep * j,
                        Power = (int)((originalList.ElementAt(i).Power ?? 0) + powerStep * j),
                        StrideLength = (originalList.ElementAt(i).StrideLength ?? 0f) + strideLengthStep * j,
                        GPSPoint = newPoint
                    });
                }
            }
            return newList;
        }

        private static List<(double x, double y)?> ProcessGpsPoints(ICollection<IActivityRecord> records, out double gpsAspectRatio)
        {
            List<GpsPoint?> points = [];
            foreach (IActivityRecord record in records)
                points.Add(record.GPSPoint);
            return GpsPoint.PointsListToUnitaryScreenSpace(points, out gpsAspectRatio);
        }
    }
}
