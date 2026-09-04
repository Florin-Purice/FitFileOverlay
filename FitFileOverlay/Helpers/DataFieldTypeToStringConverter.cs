using FitFileOverlay.Enums;
using System.Globalization;
using System.Windows.Data;

namespace FitFileOverlay.Helpers;

public class DataFieldTypeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DataFieldType df)
            return string.Empty;

        return df.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string enumValue = value as string ?? throw new ArgumentException("ExceptionParameterMustBeAnEnumName");
        if (!Enum.IsDefined(typeof(DataFieldType), enumValue))
        {
            throw new ArgumentException("ExceptionValueMustBeAnEnumValue");
        }

        return Enum.Parse<DataFieldType>(enumValue);
    }
}