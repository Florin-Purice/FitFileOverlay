using Dynastream.Fit;

using DateTime = System.DateTime;

namespace FitFileOverlay.Overlay;

public class FitRecord : IActivityRecord
{
    public FitRecord(RecordMesg recordMesg)
    {
        TimeStamp = recordMesg.GetTimestamp().GetDateTime();
        Distance = recordMesg.GetDistance();
        Speed = recordMesg.GetEnhancedSpeed();
        HeartRate = recordMesg.GetHeartRate();
    }

    public DateTime TimeStamp { get; private set; }
    public GPSPoint? GPSPoint { get; private set; }
    public int? HeartRate { get; private set; }
    public float? Speed { get; private set; }
    public float? Distance { get; private set; }
}
