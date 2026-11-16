using FitFileOverlay.Core.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Core.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum DistanceUnit
{
    [Description("km")] Kilometers,
    [Description("mi")] Miles
}
