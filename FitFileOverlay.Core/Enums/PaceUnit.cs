using FitFileOverlay.Core.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Core.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum PaceUnit
{
    [Description("min/km")] MinutesPerKilometer,
    [Description("min/mi")] MinutesPerMile
}
