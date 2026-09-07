using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum PaceUnit
{
    [Description("min/km")] MinutesPerKilometer,
    [Description("min/mi")] MinutesPerMile
}
