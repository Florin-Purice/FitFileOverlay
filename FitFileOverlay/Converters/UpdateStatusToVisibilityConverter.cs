using FitFileOverlay.Enums;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

namespace FitFileOverlay.Converters;

public class UpdateStatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not UpdateStatus updateStatus || parameter is not string paramString)
            return Visibility.Collapsed;
        if (GetEnumValueName(updateStatus) == paramString)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string GetEnumValueName(UpdateStatus enumValue)
    {
        FieldInfo? field = enumValue.GetType().GetField(enumValue.ToString());
        DescriptionAttribute? desc = field?.GetCustomAttribute<DescriptionAttribute>();
        return desc?.Description ?? enumValue.ToString();
    }
}
