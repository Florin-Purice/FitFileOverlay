using SkiaSharp;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FitFileOverlay.Helpers;

public class SKColorToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || value is not SKColor)
            throw new ArgumentException("value is not a SKColor object");
        SKColor color = (SKColor)value;
        return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || value is not Color)
            throw new ArgumentException("value is not a Color object");
        Color color = (Color)value;
        return new SKColor(color.R, color.G, color.B, color.A);
    }
}