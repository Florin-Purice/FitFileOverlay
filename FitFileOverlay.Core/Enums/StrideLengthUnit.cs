using FitFileOverlay.Core.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Core.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum StrideLengthUnit
{
    [Description("m")] Meters,
    [Description("ft")] Feet,
    [Description("cm")] Centimeters,
    [Description("in")] Inches
}
