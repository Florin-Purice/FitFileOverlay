using Dynastream.Fit;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using DateTime = Dynastream.Fit.DateTime;
using Font = System.Drawing.Font;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace GarminFitFilePaceOverlay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel;
        FitOverlayProcessor? fitProcessor;
        bool snapshotLock = false;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = (DataContext as MainWindowViewModel) ?? new MainWindowViewModel();
            viewModel.SnapshotActivityPercentChanged += ViewModel_SnapshotActivityPercentChanged;
        }

        private async void ProcessFitFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "FIT Files|*.fit";
            if (ofd.ShowDialog() == true)
            {
                //Disable window interraction
                viewModel.IsEnabled = false;
                FitOverlayProcessor fop = new FitOverlayProcessor(ofd.FileName);
                if (fop.IsValid)
                {
                    fitProcessor = fop;
                    int recordIndex = (int)(fitProcessor.RecordsCount * viewModel.SnapshotActivityPercent);
                    if (recordIndex >= fitProcessor.RecordsCount)
                        recordIndex = fitProcessor.RecordsCount - 1;
                    SKBitmap snapshot = fitProcessor.GetSnapshotAtRecord(recordIndex);
                    WriteableBitmap writeableBitmap = snapshot.ToWriteableBitmap();
                    snapshotImage.Source = writeableBitmap;
                    //log result
                    LogMessage($"FIT file loaded successfully. Activity duration: {fitProcessor.ActivityDurationString} LTHR: {fitProcessor.FileLTHR}", System.Windows.Media.Brushes.Green);
                }
                else
                    LogMessage($"Failed to process FIT file. Error: {fop.ErrorMessage}", System.Windows.Media.Brushes.Red);
                //Re-enable window interaction
                viewModel.IsEnabled = true;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (fitProcessor != null && fitProcessor.IsValid)
            {
                //Disable window interraction
                viewModel.IsEnabled = false;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "mov Video|*.mov";
                if (sfd.ShowDialog() == true)
                {
                    //set up progress logging
                    fitProcessor.LogProgress += FitProcessor_LogProgress;
                    System.DateTime startTime = System.DateTime.Now;
                    string outputPath = sfd.FileName;
                    await fitProcessor.ExportVideo(outputPath);
                    //remove progress log handler and log completion
                    fitProcessor.LogProgress -= FitProcessor_LogProgress;
                    TimeSpan elapsed = System.DateTime.Now - startTime;
                    LogMessage($"Finished exporting video in {elapsed.ToString("%h'h'%m'm'%s's'")}.", System.Windows.Media.Brushes.Green);

                }
                //Re-enable window interaction
                viewModel.IsEnabled = true;
            }
            else
                LogMessage("No valid FIT file loaded. Please load a FIT file first.", System.Windows.Media.Brushes.Red);
        }

        private async void ViewModel_SnapshotActivityPercentChanged(double newValue)
        {
            if (!snapshotLock && fitProcessor != null && fitProcessor.IsValid)
            {
                snapshotLock = true;
                int recordIndex = (int)(fitProcessor.RecordsCount * newValue);
                if(recordIndex >= fitProcessor.RecordsCount)
                    recordIndex = fitProcessor.RecordsCount - 1;
                SKBitmap snapshot = await Task.Run<SKBitmap>(() => fitProcessor.GetSnapshotAtRecord(recordIndex));
                WriteableBitmap writeableBitmap = snapshot.ToWriteableBitmap();
                snapshotImage.Source = writeableBitmap;
                snapshotLock = false;
            }
        }

        private void FitProcessor_LogProgress(FitOverlayProcessor sender, FitOverlayProcessor.LogProgressEventArgs e)
        {
            LogMessage($"Exporting video: {(e.Progress * 100f).ToString("0.0")}% | Elapsed: {e.Elapsed.ToString("%h'h'%m'm'%s's'")} | Remaining: {e.Remaining.ToString("%h'h'%m'm'%s's'")}", System.Windows.Media.Brushes.Blue);
        }

        private void LogMessage(string message, System.Windows.Media.Brush color)
        {
            Dispatcher?.Invoke(() =>
            {
                logFlowDoc.Blocks.Clear();
                logFlowDoc.Blocks.Add(new Paragraph(new Run(message) { Foreground = color }));
            });
        }

        bool xdd = true;
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            bool embedded = false;
            string testTemplateFileName;
            (testTemplateFileName, embedded) = (xdd = !xdd) ? ("TestTemplate.xaml", false) : ("DefaultTemplate.xaml", true);
            Settings.LoadTemplate(testTemplateFileName, embedded);
        }
    }
}