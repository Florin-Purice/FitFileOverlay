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
        TimeStamp = recordMesg.GetTimestamp().GetDateTime();
        Distance = recordMesg.GetDistance();
        Speed = recordMesg.GetEnhancedSpeed();
        HeartRate = recordMesg.GetHeartRate();
        GPSPoint = GpsPoint.FromFitRecord(recordMesg);
    }

    public DateTime TimeStamp { get; set; }
    public GpsPoint? GPSPoint { get; set; }
    public int? HeartRate { get; set; }
    public float? Speed { get; set; }
    public float? Distance { get; set; }
}
