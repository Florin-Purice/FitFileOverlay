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
            List<IActivityRecord> records = [];
            //insert interpolated records if needed
            if (Settings.FPS > 1)
                records = InterpolateRecords(File.Records, Settings.FPS);
            else
                records = File.Records;
            InstanceData data = new() { Records = records };
            PrepareInstanceData(ref data);

            //Generate video frames and encode video using FFMpegCore
            IEnumerable<IVideoFrame> frames = CreateVideoFrames(data, progressReportCallback);
            RawVideoPipeSource framesSource = new(frames) { FrameRate = Settings.FPS };
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
        if (File == null || !File.IsValid || Settings == null)
            return null;
        if (recordIndex < 0 || recordIndex >= File.Records.Count)
            return null;
        if (!Settings.IsGpsOverlayEnabled && !Settings.IsDataFieldsOverlayEnabled && !Settings.IsAltitudeOverlayEnabled)
            return null;

        InstanceData data = new() { Records = File.Records };
        PrepareInstanceData(ref data);
        data.PathRendererOptions.FadePointCount = Settings.FadeDurationSeconds;
        data.GraphRendererOptions.FadePointCount = Settings.FadeDurationSeconds;

        SKBitmap? pathCacheBitmap = null;
        SKBitmap? altitudeCacheBitmap = null;
        return CreateFrame(data, recordIndex, ref pathCacheBitmap, ref altitudeCacheBitmap);
    }

    private IEnumerable<IVideoFrame> CreateVideoFrames(InstanceData data, Action<double>? progressReportCallback = null)
    {
        SKBitmap? pathCacheBitmap = null;
        SKBitmap? altitudeCacheBitmap = null;
        for (int i = 0; i < data.Records.Count; ++i)
        {
            SKBitmap frame = CreateFrame(data, i, ref pathCacheBitmap, ref altitudeCacheBitmap);
            progressReportCallback?.Invoke((double)i / data.Records.Count);
            yield return new BitmapVideoFrameWrapper(frame);
        }
    }

    private SKBitmap CreateFrame(InstanceData data, int recordIndex, ref SKBitmap? pathCacheBitmap, ref SKBitmap? altitudeCacheBitmap)
    {
        //create underlying bitmap
        SKBitmap sKBitmap = new(data.OverlayWidth, data.OverlayHeight);
        SKCanvas sKCanvas = new(sKBitmap);
        sKCanvas.Clear(Settings!.Background);
        if (Settings.IsDataFieldsOverlayEnabled)
        {
            //create data fields overlay and apply
            SKBitmap? dataFieldsOverlay = CreateDataFieldsOverlay(data.Records[recordIndex]);
            if (dataFieldsOverlay != null && !dataFieldsOverlay.IsEmpty)
                sKCanvas.DrawBitmap(dataFieldsOverlay, 0, 0, SKSamplingOptions.Default);
        }
        if (Settings.IsGpsOverlayEnabled)
        {
            //apply base gps overlay
            sKCanvas.DrawBitmap(data.GpsBaseBitmap, data.MapOverlayStartX, 0, SKSamplingOptions.Default);
            //create partial gps path and apply over base gps overlay
            SKBitmap gpsPathOverlay = PathRenderer.RenderTrailPart(data.PathRendererOptions, data.DrawPoints, recordIndex, ref pathCacheBitmap);
            sKCanvas.DrawBitmap(gpsPathOverlay, data.MapOverlayStartX, 0, SKSamplingOptions.Default);
        }
        if (Settings.IsAltitudeOverlayEnabled)
        {
            //apply base altitude overlay
            sKCanvas.DrawBitmap(data.AltitudeBaseBitmap, 0, data.AltitudeOverlayStartY, SKSamplingOptions.Default);
            //create partial altitude path and apply over base altitude overlay
            SKBitmap altitudePathOverlay = GraphRenderer.RenderTrailPart(data.GraphRendererOptions, data.AltitudeValues, data.AltitudeXPositions, recordIndex, GetAltitudeValueConverter(Settings.AltitudeUnit), ref altitudeCacheBitmap);
            sKCanvas.DrawBitmap(altitudePathOverlay, 0, data.AltitudeOverlayStartY, SKSamplingOptions.Default);
        }
        return sKBitmap;
    }

    private void PrepareInstanceData(ref InstanceData data)
    {
        CalculateLayout(ref data);

        data.PathRendererOptions = CreatePathRendererOptionsFromSettings(Settings!);
        data.GraphRendererOptions = CreateGraphRendererOptionsFromSettings(Settings!);

        data.DrawPoints = [];
        data.AltitudeValues = [];
        data.AltitudeXPositions = [];
        data.GpsBaseBitmap = null;
        data.AltitudeBaseBitmap = null;

        if (Settings!.IsGpsOverlayEnabled)
            PrepareGpsOverlayData(ref data);

        if (Settings.IsAltitudeOverlayEnabled)
            PrepareAltitudeOverlayData(ref data);
    }

    private void PrepareGpsOverlayData(ref InstanceData data)
    {
        //create list of unitary screenspace gps points
        List<(double x, double y)?> normalizedGpsPoints = ProcessGpsPoints(data.Records, out double gpsAspectRatio);
        float gpsDrawAreaPadding = Settings!.GpsLineWidth * 2;//add some padding so the points on the border dont get cut off
        double gpsDrawAreaAspectRatio = (double)(data.PathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2) / (data.PathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2);
        double scale;
        if (gpsDrawAreaAspectRatio > gpsAspectRatio)
            scale = data.PathRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2;// points cover the full height
        else
            scale = data.PathRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2;// points cover the full width
        //transform points into actual draw points
        foreach ((double x, double y)? point in normalizedGpsPoints)
            if (point is null)
                data.DrawPoints.Add(null);
            else
            {
                float x = (float)((point?.x ?? 0) * scale + gpsDrawAreaPadding);
                float y = (float)((point?.y ?? 0) * scale + gpsDrawAreaPadding);
                data.DrawPoints.Add(new SKPoint(x, y));
            }
        //create base gps overlay
        data.GpsBaseBitmap = PathRenderer.RenderStaticPart(data.PathRendererOptions, data.DrawPoints);
    }

    private void PrepareAltitudeOverlayData(ref InstanceData data)
    {
        float? totalDistance = data.Records.Last().Distance;
        for (int i = 0; i < data.Records.Count; i++)
        {
            data.AltitudeValues.Add(data.Records[i].Altitude);
            float? xPos = Settings!.AltitudeXReference switch
            {
                AltitudeXReference.Distance => data.Records[i].Distance / totalDistance,
                _ => (float)i / (data.Records.Count - 1)
            };
            data.AltitudeXPositions.Add(xPos);
        }
        data.AltitudeBaseBitmap = GraphRenderer.RenderStaticPart(data.GraphRendererOptions, data.AltitudeValues, data.AltitudeXPositions);
    }

    private void CalculateLayout(ref InstanceData data)
    {
        data.OverlayWidth = 0;
        data.OverlayHeight = 0;
        data.AltitudeOverlayStartY = 0;
        data.MapOverlayStartX = 0;
        if (Settings!.IsDataFieldsOverlayEnabled)
        {
            data.OverlayWidth = Settings.DataFieldsOverlayWidth;
            data.OverlayHeight = Settings.DataFieldsOverlayHeight;
        }
        if (Settings.IsGpsOverlayEnabled)
        {
            data.MapOverlayStartX = data.OverlayWidth;
            data.OverlayWidth += Settings.GpsOverlayWidth;
            data.OverlayHeight = Math.Max(data.OverlayHeight, Settings.GpsOverlayHeight);
        }
        if (Settings.IsAltitudeOverlayEnabled)
        {
            data.OverlayWidth = Math.Max(data.OverlayWidth, Settings.AltitudeOverlayWidth);
            data.AltitudeOverlayStartY = data.OverlayHeight;
            data.OverlayHeight += Settings.AltitudeOverlayHeight;
        }
    }

    private SKBitmap? CreateDataFieldsOverlay(IActivityRecord record)
    {
        DataFieldRendererOptions rendererOptions = CreateDataFieldRendererOptionsFromSettings(Settings!);
        int dataFieldCount = Settings!.DrawnDataFields.Count;
        int dataFieldsPerColumn = (int)Math.Ceiling((double)dataFieldCount / Settings.DataOverlayColumnCount);
        if (Settings.DataFieldsOverlayWidth <= 0)
            return null;
        SKBitmap sKBitmap = new(Settings.DataFieldsOverlayWidth, Settings.DataFieldsOverlayHeight);
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
                value = ConvertSpeedToPaceString(record.Speed, Settings!.PaceUnit);
                unit = Settings.PaceUnit == PaceUnit.MinutesPerKilometer ? "/km" : "/mi";
                if (Settings.UppercasePaceUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.HeartRate:
                label = Settings!.HrLabel;
                value = record.HeartRate.ToString() ?? string.Empty;
                unit = Settings.UppercaseHrUnit ? "BPM" : "bpm";
                rendererOptionsBase.ValueColor = GetHeartRateZoneBrush(record.HeartRate ?? 0);
                break;
            case DataFieldType.Distance:
                label = Settings!.DistanceLabel;
                switch (Settings.DistanceUnit)
                {
                    case DistanceUnit.Miles:
                        //meters to miles
                        value = (record.Distance / 1609.34)?.ToString("0.00") ?? string.Empty;
                        unit = "mi";
                        break;
                    case DistanceUnit.Kilometers:
                    default:
                        //meters to kilometers
                        value = (record.Distance / 1000)?.ToString("0.00") ?? string.Empty;
                        unit = "km";
                        break;
                }
                if (Settings.UppercaseDistanceUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.Cadence:
                label = Settings!.CadenceLabel;
                //half cadence (rpm) to full cadence (spm)
                value = (record.Cadence * 2)?.ToString("0") ?? string.Empty;
                unit = Settings.UppercaseCadenceUnit ? "SPM" : "spm";
                break;
            case DataFieldType.Speed:
                label = Settings!.SpeedLabel;
                switch (Settings!.SpeedUnit)
                {
                    case SpeedUnit.MilesPerHour:
                        // m/s to mph
                        value = (record.Speed * 2.23694)?.ToString("0.0") ?? string.Empty;
                        unit = "mph";
                        break;
                    case SpeedUnit.FeetPerSecond:
                        // m/s to ft/s
                        value = (record.Speed * 3.28084)?.ToString("0.0") ?? string.Empty;
                        unit = "ft/s";
                        break;
                    case SpeedUnit.MetersPerSecond:
                        // m/s
                        value = record.Speed?.ToString("0.0") ?? string.Empty;
                        unit = "m/s";
                        break;
                    case SpeedUnit.KilometersPerHour:
                    default:
                        // m/s to km/h
                        value = (record.Speed * 3.6)?.ToString("0.0") ?? string.Empty;
                        unit = "km/h";
                        break;
                }
                if (Settings.UppercaseSpeedUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.Power:
                label = Settings!.PowerLabel;
                value = record.Power?.ToString() ?? string.Empty;
                unit = "W";
                break;
            case DataFieldType.StrideLength:
                label = Settings!.StrideLengthLabel;
                switch (Settings!.StrideLengthUnit)
                {
                    case StrideLengthUnit.Feet:
                        // milimeters to feet
                        value = (record.StrideLength / 304.8)?.ToString("0.00") ?? string.Empty;
                        unit = "ft";
                        break;
                    case StrideLengthUnit.Centimeters:
                        // milimeters to centimeters
                        value = (record.StrideLength / 10)?.ToString("0") ?? string.Empty;
                        unit = "cm";
                        break;
                    case StrideLengthUnit.Inches:
                        // milimeters to inches
                        value = (record.StrideLength / 25.4)?.ToString("0.0") ?? string.Empty;
                        unit = "in";
                        break;
                    case StrideLengthUnit.Meters:
                    default:
                        // milimeters to meters
                        value = (record.StrideLength / 1000)?.ToString("0.00") ?? string.Empty;
                        unit = "m";
                        break;
                }
                if (Settings.UppercaseStrideLengthUnit)
                    unit = unit.ToUpper();
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
            int secondsBetweenRecords = (int)(originalList[i + 1].TimeStamp - originalList[i].TimeStamp).TotalSeconds;
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
            float altitudeStep = ((originalList[i + 1].Altitude - originalList[i].Altitude) ?? 0f) / interpolatedRecordCount;
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
                    Altitude = (originalList[i].Altitude ?? 0f) + altitudeStep * j,
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
        PathRendererOptions pathRendererOptions = new()
        {
            BitmapWidth = settings.GpsOverlayWidth,
            BitmapHeight = settings.GpsOverlayHeight,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            TertiaryColor = settings.TertiaryColor,
            StrokeWidth = settings.GpsLineWidth,
            FadePointCount = (int)(settings.FadeDurationSeconds * settings.FPS)
        };
        return pathRendererOptions;
    }

    private static GraphRendererOptions CreateGraphRendererOptionsFromSettings(OverlaySettings settings)
    {
        GraphRendererOptions graphRendererOptions = new()
        {
            BitmapWidth = settings.AltitudeOverlayWidth,
            BitmapHeight = settings.AltitudeOverlayHeight,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            TertiaryColor = settings.TertiaryColor,
            BackgroundAlpha = settings.AltitudeBackgroundAlpha,
            StrokeWidth = settings.AltitudeLineWidth,
            BottomPaddingPercent = settings.AltitudeBottomPaddingPercent,
            MinimumRange = settings.AltitudeMinimumRange,
            ValueFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.ValueFontFamily,
                    weight: settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.AltitudeValueFontSize),
            UnitFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.UnitFontFamily,
                    weight: settings.IsUnitFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsUnitFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.AltitudeUnitFontSize),
            UnitText = settings.AltitudeUnit switch
            {
                AltitudeUnit.Feet => "ft",
                _ => "m"
            },
            IsTextEnabled = settings.IsAltitudeTextEnabled,
            FadePointCount = (int)(settings.FadeDurationSeconds * settings.FPS)
        };
        if (settings.UppercaseAltitudeUnit)
            graphRendererOptions.UnitText = graphRendererOptions.UnitText.ToUpper();
        return graphRendererOptions;
    }

    private static string ConvertSpeedToPaceString(float? speed, PaceUnit unit)
    {
        var secPerUnitDistance = unit switch
        {
            PaceUnit.MinutesPerMile => (int)(1609.34 / (speed ?? 0f)),
            _ => (int)(1000 / (speed ?? 0f)),
        };
        string paceString;
        if (secPerUnitDistance < 1 || secPerUnitDistance > 3600)
            paceString = "--'--\"";
        else
            paceString = $"{secPerUnitDistance / 60}'{secPerUnitDistance % 60:D2}\"";
        return paceString;
    }

    private static List<(double x, double y)?> ProcessGpsPoints(ICollection<IActivityRecord> records, out double gpsAspectRatio)
    {
        List<GpsPoint?> points = [];
        foreach (IActivityRecord record in records)
            points.Add(record.GPSPoint);
        return GpsPoint.PointsListToUnitaryScreenSpace(points, out gpsAspectRatio);
    }

    private static Func<float?, float?> GetAltitudeValueConverter(AltitudeUnit unit)
    {
        return unit switch
        {
            AltitudeUnit.Feet => (x) => x * 3.28084f,
            _ => (x) => x //default meters
        };
    }

    #endregion

    private struct InstanceData
    {
        public int OverlayWidth;
        public int OverlayHeight;
        public int AltitudeOverlayStartY;
        public int MapOverlayStartX;
        public PathRendererOptions PathRendererOptions;
        public GraphRendererOptions GraphRendererOptions;
        public List<IActivityRecord> Records;
        public List<SKPoint?> DrawPoints;
        public List<float?> AltitudeValues;
        public List<float?> AltitudeXPositions;
        public SKBitmap? GpsBaseBitmap;
        public SKBitmap? AltitudeBaseBitmap;
    }
}
