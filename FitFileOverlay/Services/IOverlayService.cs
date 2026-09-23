using FitFileOverlay.Models;
using SkiaSharp;
using System.ComponentModel;

namespace FitFileOverlay.Services;

public interface IOverlayService : INotifyPropertyChanged
{
    event Action? NewFileLoaded;
    event NewSettingsAppiedEventHandler? NewSettingsApplied;
    event Action? CropIntervalChanged;

    OverlaySettings? Settings { get; set; }
    FitFile? File { get; }
    /// <summary>
    /// Record index at the start of the crop (inclusive)
    /// </summary>
    public int CropStartIndex { get; set; }
    /// <summary>
    /// Record index at the end of the crop (exclusive)
    /// </summary>
    public int CropEndIndex { get; set; }

    bool Load(string fileName);
    Task Export(string outputPath, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null);
    SKBitmap? GetSnapshot(double activityPercent);
}

public delegate void NewSettingsAppiedEventHandler(OverlaySettings? oldValue,  OverlaySettings? newValue);
