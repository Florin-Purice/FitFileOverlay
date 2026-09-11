using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AltitudeUnit
{
    [Description("m")] Meters,
    [Description("ft")] Feet
}

