using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace FitFileOverlay.Converters;

public class EnumDescriptionTypeConverter(Type type) : EnumConverter(type)
{
    public override object? ConvertTo(ITypeDescriptorContext? ctx, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Enum e)
        {
            FieldInfo? field = e.GetType().GetField(e.ToString());
            DescriptionAttribute? desc = field?.GetCustomAttribute<DescriptionAttribute>();
            return desc?.Description ?? e.ToString();
        }
        return base.ConvertTo(ctx, culture, value, destinationType);
    }
}
