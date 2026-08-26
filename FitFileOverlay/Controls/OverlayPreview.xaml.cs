using CommunityToolkit.Mvvm.ComponentModel;
using FitFileOverlay.Overlay;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System.ComponentModel;
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

    private static void ActivityPercentChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        _ = Task.Run(((OverlayPreview)d).UpdateSnapshotImage);
    }

    private static void OverlayServiceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        OverlayPreview op = (OverlayPreview)d;
        IOverlayService os = (IOverlayService)e.NewValue;
        os?.NewFileLoaded += op.OnNewFileLoaded;
    }

    private void OnNewFileLoaded()
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
