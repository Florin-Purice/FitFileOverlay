using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Services;

public partial class OverlayService : ObservableObject, IOverlayService
{
    public OverlayService(OverlaySettings overlaySettings)
    {
        Settings = overlaySettings;
    }

    public event Action? NewFileLoaded;
    public event NewSettingsAppiedEventHandler? NewSettingsApplied;

    [ObservableProperty]
    public partial OverlayProcessor? Processor { get; private set; }
    [ObservableProperty]
    public partial OverlaySettings? Settings { get; set; }
    [ObservableProperty]
    public partial FitFile? File { get; private set; }

    public bool Load(string fileName)
    {
        try
        {
            Processor = new(fileName);
            File = Processor.FitFile;
            NewFileLoaded?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task Export(string outputPath, Action<double>? progressReportCallback = null, CancellationToken? cancellationToken = null)
    {
        if (Processor != null)
            await Processor.ExportVideo(Settings ?? new OverlaySettings(), outputPath, progressReportCallback, cancellationToken);
    }

    public SKBitmap? GetSnapshot(double activityPercent)
    {
        return Processor?.GetSnapshotAtActivityPercent(Settings ?? new OverlaySettings(), activityPercent);
    }

    partial void OnSettingsChanged(OverlaySettings? oldValue, OverlaySettings? newValue)
    {
        NewSettingsApplied?.Invoke(oldValue, newValue);
    }
}
