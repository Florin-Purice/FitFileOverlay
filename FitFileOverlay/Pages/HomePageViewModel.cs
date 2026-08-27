using FitFileOverlay.Services;
using Microsoft.Win32;
using System.IO;

namespace FitFileOverlay.Pages;

public partial class HomePageViewModel(IOverlayService overlayService) : ObservableObject
{
    private DateTime _exportVideoStartTime;

    [ObservableProperty]
    public partial string ExportVideoProgress { get; set; } = string.Empty;

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

    [ObservableProperty]
    public partial IOverlayService OverlayService { get; private set; } = overlayService;

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
                    _ = OverlayService.Load(ofd.FileName);
                }
                catch { }
            });
            //await UpdateSnapshotImage();
        }
        //Re-enable window interaction
        IsBusy = false;
    }

    [RelayCommand(CanExecute = nameof(CanExportVideo), IncludeCancelCommand = true)]
    public async Task ExportVideo(CancellationToken cancellationToken)
    {
        if (OverlayService.File == null) return;
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
                await OverlayService.Export(sfd.FileName, ReportExportViewProgress, cancellationToken);
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
        //If exporting was canceled display a message else report success
        if (cancellationToken.IsCancellationRequested)
            ExportVideoProgress = "Export canceled.";
        else
        {
            TimeSpan exportDuration = DateTime.Now - _exportVideoStartTime;
            string durationString = exportDuration.TotalHours < 1 ? exportDuration.ToString(@"mm\:ss") : exportDuration.ToString(@"h\:mm\:ss");
            ExportVideoProgress = $"Export finished in {durationString}";
        }
    }

    private bool CanExportVideo()
    {
        return IsNotBusy && OverlayService.File != null;
    }

    private void ReportExportViewProgress(double progress)
    {
        TimeSpan exportDuration = DateTime.Now - _exportVideoStartTime;
        string durationString = exportDuration.TotalHours < 1 ? exportDuration.ToString(@"mm\:ss") : exportDuration.ToString(@"h\:mm\:ss");
        ExportVideoProgress = progress.ToString("P1") + " Elapsed time " + durationString;
    }
}