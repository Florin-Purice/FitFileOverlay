using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum DistanceUnit
{
    [Description("km")] Kilometers,
    [Description("mi")] Miles
}
