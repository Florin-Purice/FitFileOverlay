using Dynastream.Fit;

using DateTime = System.DateTime;

namespace FitFileOverlay.Models;

public class FitRecord : IActivityRecord
{
    public FitRecord()
    {
    }

    public FitRecord(RecordMesg recordMesg)
    {
        GPSPoint = GpsPoint.FromFitRecord(recordMesg);
        TimeStamp = recordMesg.GetTimestamp().GetDateTime();
        Distance = recordMesg.GetDistance();
        Speed = recordMesg.GetEnhancedSpeed();
        HeartRate = recordMesg.GetHeartRate();
        Cadence = recordMesg.GetCadence() + recordMesg.GetFractionalCadence();
    }

    public DateTime TimeStamp { get; set; }
    public GpsPoint? GPSPoint { get; set; }
    public int? HeartRate { get; set; }
    public float? Speed { get; set; }
    public float? Distance { get; set; }
    public float? Cadence { get; set; }
}
