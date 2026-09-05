using FitFileOverlay.Models;
using SkiaSharp;
using System.ComponentModel;

namespace FitFileOverlay.Services;

public interface IOverlayService : INotifyPropertyChanged
{
    event Action? NewFileLoaded;
    event NewSettingsAppiedEventHandler? NewSettingsApplied;

    OverlaySettings? Settings { get; set; }
    FitFile? File { get; }

    bool Load(string fileName);
    Task Export(string outputPath, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null);
    SKBitmap? GetSnapshot(double activityPercent);
}

public delegate void NewSettingsAppiedEventHandler(OverlaySettings? oldValue,  OverlaySettings? newValue);
