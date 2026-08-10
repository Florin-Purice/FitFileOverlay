using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminFitFilePaceOverlay.Navigation;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GarminFitFilePaceOverlay.Pages
{
    internal partial class HomePageViewModel : ViewModelBase
    {
        private INavigationService settingsPageNavigationService;
        private bool snapshotLock = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ExportVideoCommand))]
        private FitOverlayProcessor? fitProcessor;

        [ObservableProperty]
        private string logText = string.Empty;

        [ObservableProperty]
        private System.Windows.Media.Brush logTextColor = System.Windows.Media.Brushes.Black;

        [ObservableProperty]
        private WriteableBitmap? snapshotImage;

        [ObservableProperty]
        private double snapshotActivityPercent = 0.5;

        [ObservableProperty]
        private bool isExportingVideo = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        [NotifyCanExecuteChangedFor(nameof(ExportVideoCommand))]
        [NotifyCanExecuteChangedFor(nameof(LoadFileCommand))]
        private bool isBusy = false;

        public HomePageViewModel(INavigationService settingsPageNavigationService)
        {
            this.settingsPageNavigationService = settingsPageNavigationService;
        }

        public bool IsNotBusy => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanNavigateToSettingsPage))]
        public void NavigateToSettingsPage() => settingsPageNavigationService.Navigate();

        [RelayCommand(CanExecute = nameof(CanLoadFile))]
        public async Task LoadFile()
        {
            //Disable window interraction
            IsBusy = true;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "FIT Files|*.fit";
            if (ofd.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    FitOverlayProcessor fop = new FitOverlayProcessor(ofd.FileName);
                    if (fop.IsValid)
                    {
                        int recordIndex = (int)(fop.RecordsCount * SnapshotActivityPercent);
                        if (recordIndex >= fop.RecordsCount)
                            recordIndex = fop.RecordsCount - 1;
                        SKBitmap snapshot = fop.GetSnapshotAtRecord(recordIndex);
                        RunOnMainThread(() => FitProcessor = fop);
                        RunOnMainThread(() => SnapshotImage = snapshot.ToWriteableBitmap());
                        //log result
                        RunOnMainThread(() => LogMessage($"FIT file loaded successfully. Activity duration: {fop.ActivityDurationString} LTHR: {fop.FileLTHR}", System.Windows.Media.Brushes.Green));
                    }
                    else
                        RunOnMainThread(() => LogMessage($"Failed to process FIT file. Error: {fop.ErrorMessage}", System.Windows.Media.Brushes.Red));
                });
            }
            //Re-enable window interaction
            IsBusy = false;
        }

        private bool CanNavigateToSettingsPage()
        {
            return IsNotBusy;
        }

        private bool CanLoadFile()
        {
            return IsNotBusy;
        }

        [RelayCommand(CanExecute = nameof(CanExportVideo), IncludeCancelCommand = true)]
        public async Task ExportVideo(CancellationToken cancellationToken)
        {
            if (FitProcessor == null) return;
            //Disable window interraction
            IsBusy = true;
            IsExportingVideo = true;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "mov Video|*.mov";
            if (sfd.ShowDialog() == true)
            {
                //set up progress logging
                FitProcessor.LogProgress += FitProcessor_LogProgress;
                try
                {
                    System.DateTime startTime = System.DateTime.Now;
                    string outputPath = sfd.FileName;
                    await FitProcessor.ExportVideo(outputPath, cancellationToken);
                    //remove progress log handler and log completion
                    TimeSpan elapsed = System.DateTime.Now - startTime;
                    RunOnMainThread(() => LogMessage($"Finished exporting video in {elapsed.ToString("%h'h'%m'm'%s's'")}.", System.Windows.Media.Brushes.Green));
                }
                catch (OperationCanceledException)
                {
                    if(File.Exists(sfd.FileName))
                    {
                        try
                        {
                            File.Delete(sfd.FileName);
                        }
                        catch (Exception) { }
                    }
                    RunOnMainThread(() => LogMessage($"Exporting video was canceled.", System.Windows.Media.Brushes.DarkOrange));
                }
                finally
                {
                    FitProcessor.LogProgress -= FitProcessor_LogProgress;
                }

            }
            //Re-enable window interaction
            IsBusy = false;
            IsExportingVideo = false;
        }

        private bool CanExportVideo()
        {
            return IsNotBusy && FitProcessor != null && FitProcessor.IsValid;
        }

        partial void OnSnapshotActivityPercentChanged(double value)
        {
            Task.Run(async () =>
            {
                if (!snapshotLock)
                {
                    snapshotLock = true;
                    await UpdateSnapshotImage(value);
                    snapshotLock = false;
                }
            });
        }

        private void FitProcessor_LogProgress(FitOverlayProcessor sender, FitOverlayProcessor.LogProgressEventArgs e)
        {
            RunOnMainThread(() => LogMessage($"Exporting video: {(e.Progress * 100f).ToString("0.0")}% | Elapsed: {e.Elapsed.ToString("%h'h'%m'm'%s's'")} | Remaining: {e.Remaining.ToString("%h'h'%m'm'%s's'")}", System.Windows.Media.Brushes.Blue));
        }

        private void LogMessage(string message, System.Windows.Media.Brush color)
        {
            LogText = message;
            LogTextColor = color;
        }

        private async Task UpdateSnapshotImage(double value)
        {
            if (FitProcessor != null && FitProcessor.IsValid)
            {
                int recordIndex = (int)(FitProcessor.RecordsCount * value);
                if (recordIndex >= FitProcessor.RecordsCount)
                    recordIndex = FitProcessor.RecordsCount - 1;
                SKBitmap snapshot = await Task.Run<SKBitmap>(() => FitProcessor.GetSnapshotAtRecord(recordIndex));
                RunOnMainThread(() => SnapshotImage = snapshot.ToWriteableBitmap());
            }
        }

        private void RunOnMainThread(Action action)
        {
            App.Current.Dispatcher.Invoke(action);
        }
    }
}
