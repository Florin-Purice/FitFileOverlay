using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace FitFileOverlay.Controls;

/// <summary>
/// Based on: https://github.com/funwaywang/WpfRangeSlider
/// </summary>
public class RangeSlider : Control
{
    private readonly double _thumbSize = 20;

    FrameworkElement? SliderContainer;
    Thumb? StartThumb, EndThumb;
    FrameworkElement? StartArea;
    FrameworkElement? EndArea;
    FrameworkElement? SelectedTrack;

    enum SliderThumb
    {
        None,
        Start,
        End
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty StartProperty = DependencyProperty.Register("Start", typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty EndProperty = DependencyProperty.Register("End", typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty IsMoveToPointEnabledProperty = DependencyProperty.Register("IsMoveToPointEnabled", typeof(bool), typeof(RangeSlider), new FrameworkPropertyMetadata(true));
    public static readonly DependencyProperty StartThumbToolTipProperty = DependencyProperty.Register("StartThumbToolTip", typeof(object), typeof(RangeSlider));
    public static readonly DependencyProperty EndThumbToolTipProperty = DependencyProperty.Register("EndThumbToolTip", typeof(object), typeof(RangeSlider));
    public static readonly DependencyProperty StartThumbStyleProperty = DependencyProperty.Register("StartThumbStyle", typeof(Style), typeof(RangeSlider));
    public static readonly DependencyProperty EndThumbStyleProperty = DependencyProperty.Register("EndThumbStyle", typeof(Style), typeof(RangeSlider));
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(RangeSlider));

    static RangeSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
        EventManager.RegisterClassHandler(typeof(RangeSlider), Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnThumbDragDelta));
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Start
    {
        get => (double)GetValue(StartProperty);
        set => SetValue(StartProperty, value);
    }

    public double End
    {
        get => (double)GetValue(EndProperty);
        set => SetValue(EndProperty, value);
    }

    public bool IsMoveToPointEnabled
    {
        get => (bool)GetValue(IsMoveToPointEnabledProperty);
        set => SetValue(IsMoveToPointEnabledProperty, value);
    }

    public object StartThumbToolTip
    {
        get => GetValue(StartThumbToolTipProperty);
        set => SetValue(StartThumbToolTipProperty, value);
    }

    public object EndThumbToolTip
    {
        get => GetValue(EndThumbToolTipProperty);
        set => SetValue(EndThumbToolTipProperty, value);
    }

    public Style StartThumbStyle
    {
        get => (Style)GetValue(StartThumbStyleProperty);
        set => SetValue(StartThumbStyleProperty, value);
    }

    public Style EndThumbStyle
    {
        get => (Style)GetValue(EndThumbStyleProperty);
        set => SetValue(EndThumbStyleProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        SliderContainer = GetTemplateChild("PART_SliderContainer") as FrameworkElement;
        SliderContainer?.PreviewMouseDown += ViewBox_PreviewMouseDown;

        StartArea = GetTemplateChild("PART_StartArea") as FrameworkElement;
        EndArea = GetTemplateChild("PART_EndArea") as FrameworkElement;
        SelectedTrack = GetTemplateChild("PART_SelectedTrack") as FrameworkElement;
        StartThumb = GetTemplateChild("PART_StartThumb") as Thumb;
        EndThumb = GetTemplateChild("PART_EndThumb") as Thumb;
    }

    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        Size arrangeSize = base.ArrangeOverride(arrangeBounds);

        if (Maximum > Minimum && StartArea != null && EndArea != null)
        {
            double start = Math.Max(Minimum, Math.Min(Maximum, Start));
            double end = Math.Max(Minimum, Math.Min(Maximum, End));
            Rect rectStart, rectEnd, startThumbRect, endThumbRect, selectedTrackRect;
            double viewportSize = SliderContainer != null ? SliderContainer.ActualWidth : arrangeBounds.Width;
            viewportSize -= _thumbSize;
            double startPosition = (start - Minimum) / (Maximum - Minimum) * viewportSize;
            double endPosition = (end - Minimum) / (Maximum - Minimum) * viewportSize;
            rectStart = new Rect(_thumbSize / 2, 0, startPosition, arrangeBounds.Height);
            rectEnd = new Rect(endPosition, 0, viewportSize + _thumbSize / 2 - endPosition, arrangeBounds.Height);
            startThumbRect = new Rect(startPosition, rectStart.Y, _thumbSize, rectStart.Height);
            endThumbRect = new Rect(endPosition, rectEnd.Y, _thumbSize, rectEnd.Height);
            selectedTrackRect = new Rect(startPosition + _thumbSize / 2, 0, endPosition - startPosition, arrangeBounds.Height);
            StartArea?.Arrange(rectStart);
            EndArea?.Arrange(rectEnd);
            SelectedTrack?.Arrange(selectedTrackRect);
            StartThumb?.Arrange(startThumbRect);
            EndThumb?.Arrange(endThumbRect);
        }

        return arrangeSize;
    }

    private void ViewBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsReadOnly && IsMoveToPointEnabled)
        {
            if ((StartThumb != null && StartThumb.IsMouseOver) || (EndThumb != null && EndThumb.IsMouseOver))
                return;

            var point = e.GetPosition(SliderContainer);
            if (e.ChangedButton == MouseButton.Left)
            {
                MoveBlockTo(point, SliderThumb.Start);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                MoveBlockTo(point, SliderThumb.End);
            }

            e.Handled = true;
        }
    }

    private void MoveBlockTo(Point point, SliderThumb block)
    {
        double position = point.X;
        double viewportSize = SliderContainer!.ActualWidth;
        viewportSize -= _thumbSize;
        position -= _thumbSize / 2;
        if (!double.IsNaN(viewportSize) && viewportSize > 0)
        {
            double value = Math.Min(Maximum, Minimum + (position / viewportSize) * (Maximum - Minimum));
            if (block == SliderThumb.Start)
            {
                Start = Math.Min(End, value);
            }
            else if (block == SliderThumb.End)
            {
                End = Math.Max(Start, value);
            }
        }
    }

    private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is RangeSlider rs)
        {
            rs.OnThumbDragDelta(e);
        }
    }

    private void OnThumbDragDelta(DragDeltaEventArgs e)
    {
        if (!IsReadOnly && e.OriginalSource is Thumb thumb && SliderContainer != null)
        {
            double change = e.HorizontalChange / SliderContainer.ActualWidth * (Maximum - Minimum);

            if (thumb == StartThumb)
            {
                Start = Math.Max(Minimum, Math.Min(End, Start + change));
            }
            else if (thumb == EndThumb)
            {
                End = Math.Min(Maximum, Math.Max(Start, End + change));
            }
        }
    }
}
