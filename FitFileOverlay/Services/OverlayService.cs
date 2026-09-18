using FFMpegCore;
using FFMpegCore.Extensions.SkiaSharp;
using FFMpegCore.Pipes;
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
            InstanceData data = new() { Records = InterpolateRecords(File.Records, Settings.FPS) };
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

    /// <summary>
    /// Creates in-between records based on FPS and timegap between activity records
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
        // add last record as-is
        newList.Add(originalList.Last());
        return newList;
    }

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
