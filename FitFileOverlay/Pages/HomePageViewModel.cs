using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FitFileOverlay.Navigation;
using FitFileOverlay.Overlay;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.IO;
using System.Windows.Media.Imaging;

namespace FitFileOverlay.Pages;

public partial class HomePageViewModel : ViewModelBase, INavigableViewModel
{
    private bool _snapshotLock = false;
    private DateTime _exportVideoStartTime;

    [ObservableProperty]
    public partial WriteableBitmap? SnapshotImage { get; set; }

    [ObservableProperty]
    public partial string ExportVideoProgress { get; set; }

    [ObservableProperty]
    public partial string ExportVideoElapsedTime { get; set; }

    [ObservableProperty]
    public partial bool IsExportingVideo { get; set; } = false;

    [ObservableProperty]
    public partial double SnapshotActivityPercent { get; set; } = 0.5;

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
                try
                {
                    OverlayProcessor = new(ofd.FileName);
                }
                catch { }
            });
            await UpdateSnapshotImage(SnapshotActivityPercent);
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
                _exportVideoStartTime = DateTime.Now;
                await OverlayProcessor.ExportVideo(OverlaySettings.FromAppResources(), sfd.FileName, ReportExportViewProgress, cancellationToken);
                ReportExportViewProgress(1d);
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

    partial void OnSnapshotActivityPercentChanged(double value)
    {
        Task.Run(async () =>
        {
            if (!_snapshotLock)
            {
                _snapshotLock = true;
                await UpdateSnapshotImage(value);
                _snapshotLock = false;
            }
        });
    }

    private async Task UpdateSnapshotImage(double value)
    {
        if (OverlayProcessor != null)
        {
            SKBitmap? snapshot = await Task.Run(() => OverlayProcessor.GetSnapshotAtActivityPercent(OverlaySettings.FromAppResources(), SnapshotActivityPercent));
            RunOnMainThread(() => SnapshotImage = snapshot?.ToWriteableBitmap());
        }
    }

    private void ReportExportViewProgress(double progress)
    {
        ExportVideoProgress = (progress * 100).ToString("0.00");
        ExportVideoElapsedTime = (DateTime.Now - _exportVideoStartTime).TotalSeconds.ToString("Elapsed seconds 0");
    }

    private static void RunOnMainThread(Action action)
    {
        App.Current.Dispatcher.Invoke(action);
    }
}
