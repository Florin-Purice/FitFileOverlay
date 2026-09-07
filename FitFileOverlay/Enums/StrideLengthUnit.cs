using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum StrideLengthUnit
{
    [Description("m")] Meters,
    [Description("ft")] Feet,
    [Description("cm")] Centimeters,
    [Description("in")] Inches
}
