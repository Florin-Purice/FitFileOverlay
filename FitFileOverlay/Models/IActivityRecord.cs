using System;
using System.Collections.Generic;
using System.Text;

namespace FitFileOverlay.Models;

public interface IActivityRecord
{
    /// <summary>
    /// Time at which this record was created
    /// </summary>
    public DateTime TimeStamp { get; }

    /// <summary>
    /// Current location
    /// </summary>
    public GpsPoint? GPSPoint { get; }

    /// <summary>
    /// Current heart rate in beats/minute
    /// </summary>
    public int? HeartRate { get; }

    /// <summary>
    /// Current speed in m/s
    /// </summary>
    public float? Speed { get; }

    /// <summary>
    /// Total distance covered since start of activity to this record
    /// </summary>
    public float? Distance { get; }
}
