using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AppTheme
{
    [Description("System")] System,
    [Description("Light")] Light,
    [Description("Dark")] Dark,
    [Description("High Contrast")] HighContrast
}
