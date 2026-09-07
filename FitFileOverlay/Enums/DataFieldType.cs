using FitFileOverlay.Converters;
using System.ComponentModel;

namespace FitFileOverlay.Enums;

[TypeConverter(typeof(EnumDescriptionTypeConverter))]
public enum DataFieldType
{
    Pace,
    [Description("Heart Rate")] HeartRate,
    Distance,
    Timestamp,
    Cadence,
    Speed,
    Power,
    [Description("Stride Length")] StrideLength
}
