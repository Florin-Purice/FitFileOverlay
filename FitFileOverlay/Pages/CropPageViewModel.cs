using FitFileOverlay.Services;

namespace FitFileOverlay.Pages;

public partial class CropPageViewModel(IOverlayService _overlayService) : ObservableObject
{
    [ObservableProperty]
    public partial IOverlayService OverlayService { get; private set; } = _overlayService;

    [ObservableProperty]
    public partial double SnapshotActivityPercent { get; set; } = 0.5;

    [RelayCommand]
    private void ResetCrop()
    {
        OverlayService.CropStartIndex = 0;
        OverlayService.CropEndIndex = OverlayService.File != null ? OverlayService.File.Records.Count : 1;
    }
}
