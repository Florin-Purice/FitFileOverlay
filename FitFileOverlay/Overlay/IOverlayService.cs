using SkiaSharp;
using System.ComponentModel;

namespace FitFileOverlay.Overlay;

public interface IOverlayService : INotifyPropertyChanged
{
    event Action? NewFileLoaded;

    OverlayProcessor? Processor { get; }
    OverlaySettings? Settings { get; set; }
    FitFile? File { get; }

    bool Load(string fileName);
    Task Export(string outputPath, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null);
    SKBitmap? GetSnapshot(double activityPercent);
}