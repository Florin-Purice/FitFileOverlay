using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum DataFieldType
{
    [Description("Pace")] Pace,
    [Description("Heart Rate")] HeartRate,
    [Description("Distance")] Distance,
    [Description("Timestamp")] Timestamp,
    [Description("Cadence")] Cadence,
    [Description("Speed")] Speed,
    [Description("Power")] Power,
    [Description("Stride Length")] StrideLength
}
