using FitFileOverlay.Core.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Core.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum AltitudeUnit
{
    [Description("m")] Meters,
    [Description("ft")] Feet
}

