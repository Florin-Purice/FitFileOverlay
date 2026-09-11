using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FitFileOverlay.Converters;

public class AlphaToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not byte alpha)
            throw new ArgumentException("value is not a byte");
        return Color.FromArgb(alpha, 0, 0, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color color)
            throw new ArgumentException("value is not a System.Windows.Media.Color");
        return color.A;
    }
}
