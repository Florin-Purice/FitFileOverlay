using FitFileOverlay.Core;

namespace FitFileOverlay.UI.Windows;

public partial class PreviewWindowViewModel(IOverlayService overlayService) : ObservableObject
{
    [ObservableProperty]
    public partial IOverlayService OverlayService { get; set; } = overlayService;

    [ObservableProperty]
    public partial double SnapshotPercent { get; set; } = 0.5d;
}
