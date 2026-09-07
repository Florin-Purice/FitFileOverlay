using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum SpeedUnit
{
    [Description("km/h")] KilometersPerHour,
    [Description("mph")] MilesPerHour,
    [Description("m/s")] MetersPerSecond,
    [Description("ft/s")] FeetPerSecond
}
