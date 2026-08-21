using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using FitFileOverlay.Navigation;
using FitFileOverlay.Overlay;

using Microsoft.Win32;

using SkiaSharp;

namespace FitFileOverlay.Pages;

public partial class HomePageViewModel : ViewModelBase, INavigableViewModel
{
    [ObservableProperty]
    public partial bool IsExportingVideo { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(ExportVideoCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
    public partial bool IsBusy { get; set; } = false;

    public bool IsNotBusy => !IsBusy;
    public OverlayProcessor? OverlayProcessor { get; set; }

    private bool CanLoadFile()
    {
        return IsNotBusy;
    }

    [RelayCommand(CanExecute = nameof(CanLoadFile))]
    public async Task LoadFile()
    {
        //Disable window interraction
        IsBusy = true;
        OpenFileDialog ofd = new()
        {
            Filter = "FIT Files|*.fit"
        };
        if (ofd.ShowDialog() == true)
        {
            await Task.Run(() =>
            {
                OverlayProcessor = new(ofd.FileName);
            });
        }
        //Re-enable window interaction
        IsBusy = false;
    }

    [RelayCommand(CanExecute = nameof(CanExportVideo), IncludeCancelCommand = true)]
    public async Task ExportVideo(CancellationToken cancellationToken)
    {
        if (OverlayProcessor == null) return;
        //Disable window interraction
        IsBusy = true;
        IsExportingVideo = true;
        SaveFileDialog sfd = new()
        {
            Filter = "mov Video|*.mov"
        };
        if (sfd.ShowDialog() == true)
        {
            try
            {
                string outputPath = sfd.FileName;
                OverlaySettings settings = new();
                await OverlayProcessor.ExportVideo(settings, outputPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(sfd.FileName))
                {
                    try
                    {
                        File.Delete(sfd.FileName);
                    }
                    catch (Exception) { }
                }
            }
        }
        //Re-enable window interaction
        IsBusy = false;
        IsExportingVideo = false;
    }

    private bool CanExportVideo()
    {
        return IsNotBusy && OverlayProcessor != null;
    }
}
