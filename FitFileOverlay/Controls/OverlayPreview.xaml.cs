using FitFileOverlay.Models;
using FitFileOverlay.Services;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FitFileOverlay.Controls;

/// <summary>
/// Interaction logic for OverlayPreview.xaml
/// </summary>
[ObservableObject]
public partial class OverlayPreview : UserControl
{
    public static readonly DependencyProperty OverlayServiceProperty =
        DependencyProperty.Register(nameof(OverlayService), typeof(IOverlayService), typeof(OverlayPreview),
            new PropertyMetadata(propertyChangedCallback: OverlayServiceChangedCallback));
    public static readonly DependencyProperty ActivityPercentProperty =
        DependencyProperty.Register(nameof(ActivityPercent), typeof(double), typeof(OverlayPreview),
            new FrameworkPropertyMetadata(
                    defaultValue: -1d,//to force refresh when navigating back to the view containing this control
                    flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    propertyChangedCallback: ActivityPercentChangedCallback,
                    coerceValueCallback: null,
                    isAnimationProhibited: true,
                    defaultUpdateSourceTrigger: UpdateSourceTrigger.PropertyChanged));

    private bool _snapshotLock = false;

    public OverlayPreview()
    {
        InitializeComponent();
    }

    public double ActivityPercent
    {
        get { return (double)GetValue(ActivityPercentProperty); }
        set { SetValue(ActivityPercentProperty, value); }
    }

    public IOverlayService OverlayService
    {
        get { return (IOverlayService)GetValue(OverlayServiceProperty); }
        set { SetValue(OverlayServiceProperty, value); }
    }

    public double ActivityPercentThreadSafe => Dispatcher.Invoke(new Func<double>(() => ActivityPercent));
    public IOverlayService OverlayServiceThreadSafe => Dispatcher.Invoke(new Func<IOverlayService>(() => OverlayService));

    [ObservableProperty]
    public partial WriteableBitmap? SnapshotImage { get; set; }
    [ObservableProperty]
    public partial bool IsCropped { get; set; } = false;
    [ObservableProperty]
    public partial TimeSpan CropStartTime { get; set; } = TimeSpan.Zero;
    [ObservableProperty]
    public partial TimeSpan CropEndTime { get; set; } = TimeSpan.Zero;
    [ObservableProperty]
    public partial TimeSpan CropDuration { get; set; } = TimeSpan.Zero;
    [ObservableProperty]
    public partial float CropStartDistance { get; set; } = 0f;
    [ObservableProperty]
    public partial float CropEndDistance { get; set; } = 0f;
    [ObservableProperty]
    public partial float CropTotalDistance { get; set; } = 0f;

    private static void ActivityPercentChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        _ = Task.Run(((OverlayPreview)d).UpdateSnapshotImage);
    }

    private static void OverlayServiceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        OverlayPreview op = (OverlayPreview)d;
        IOverlayService os = (IOverlayService)e.NewValue;
        os?.NewFileLoaded += op.OnNewFileLoaded;
        os?.NewSettingsApplied += op.OnNewSettingsApplied;
        os?.CropIntervalChanged += op.OnCropIntervalChanged;
        os?.Settings?.PropertyChanged += op.OnOverlayPreviewPropertyChanged;
    }

    private void OnNewSettingsApplied(OverlaySettings? oldValue, OverlaySettings? newValue)
    {
        oldValue?.PropertyChanged -= OnOverlayPreviewPropertyChanged;
        newValue?.PropertyChanged += OnOverlayPreviewPropertyChanged;
        _ = Task.Run(UpdateSnapshotImage);
    }

    private void OnNewFileLoaded()
    {
        _ = Task.Run(UpdateSnapshotImage);
    }

    private void OnCropIntervalChanged()
    {
        IOverlayService os = OverlayServiceThreadSafe;
        if(os == null || os.File == null || os.CropEndIndex < 1)
        {
            IsCropped = false;
            return;
        }
        if (os.CropStartIndex == 0 && os.CropEndIndex == os.File.Records.Count)
        {
            IsCropped = false;
            return;
        }
        IsCropped = true;
        IActivityRecord cropStartRecord = os.File.Records[os.CropStartIndex];
        IActivityRecord cropEndRecord = os.File.Records[os.CropEndIndex - 1];
        IActivityRecord firstRecord = os.File.Records.First();
        CropStartTime = cropStartRecord.TimeStamp - firstRecord.TimeStamp;
        CropEndTime = cropEndRecord.TimeStamp - firstRecord.TimeStamp;
        CropDuration = cropEndRecord.TimeStamp - cropStartRecord.TimeStamp;
        CropStartDistance = (cropStartRecord.Distance / 1000f) ?? 0f;
        CropEndDistance = (cropEndRecord.Distance / 1000f) ?? 0f;
        CropTotalDistance = ((cropEndRecord.Distance - cropStartRecord.Distance) / 1000f) ?? 0f;

        _ = Task.Run(UpdateSnapshotImage);
    }

    private void OnOverlayPreviewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = Task.Run(UpdateSnapshotImage);
    }

    private async Task UpdateSnapshotImage()
    {
        IOverlayService os = OverlayServiceThreadSafe;
        if (!_snapshotLock && os != null && os.File != null)
        {
            _snapshotLock = true;
            try
            {
                SKBitmap? snapshot = await Task.Run(() => os.GetSnapshot(ActivityPercentThreadSafe));
                RunOnMainThread(() => SnapshotImage = snapshot?.ToWriteableBitmap());
            }
            catch { }
            finally { _snapshotLock = false; }
        }
    }

    private void RunOnMainThread(Action action)
    {
        Dispatcher.Invoke(action);
    }
}
