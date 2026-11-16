using FitFileOverlay.Core.Enums;
using System.Globalization;
using System.Windows.Data;

namespace FitFileOverlay.UI.Converters;

public class DrawnFieldListToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //Checks if the DataFieldType object given as a parameter is in the list of drawn data fields
        if (value is ICollection<DataFieldType> drawnDataFields && parameter is DataFieldType field)
            return drawnDataFields.Contains(field);

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
