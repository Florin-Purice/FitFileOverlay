using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FitFileOverlay.Helpers;

public class CollectionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ICollection collection)
            return Visibility.Collapsed;
        if(collection.Count > 0)
            return Visibility.Visible;
        else
            return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
