using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using SkiaSharp;

namespace FitFileOverlay.Utils;

public class SolidColorBrushToSKColorConverter : IValueConverter
{
    public object Convert(object value, Type? targetType = null, object? parameter = null, CultureInfo? culture = null)
    {
        if(value == null || value is not SolidColorBrush)
            throw new Exception("value is not a SolidColorBrush object");
        SolidColorBrush brush = (SolidColorBrush)value;
        return new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);
    }

    public object ConvertBack(object value, Type? targetType = null, object? parameter = null, CultureInfo? culture = null)
    {
        if (value == null || value is not SKColor)
            throw new Exception("value is not a SKColor object");
        SKColor color = (SKColor)value;
        return new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue));
    }
}
