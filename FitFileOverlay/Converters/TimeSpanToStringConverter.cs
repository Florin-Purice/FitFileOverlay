using System.Globalization;
using System.Windows.Data;

namespace FitFileOverlay.Converters;

public class TimeSpanToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan timespan)
            return string.Empty;
        return timespan.TotalHours >= 1 ? timespan.ToString(@"h\:mm\:ss") : timespan.ToString(@"mm\:ss");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
