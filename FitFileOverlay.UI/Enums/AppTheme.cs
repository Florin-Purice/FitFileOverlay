using FitFileOverlay.Core.Converters;
using System.ComponentModel;

namespace FitFileOverlay.UI.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AppTheme
{
    [Description("System")] System,
    [Description("Light")] Light,
    [Description("Dark")] Dark,
    [Description("High Contrast")] HighContrast
}
