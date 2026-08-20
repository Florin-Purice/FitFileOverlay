using Dynastream.Fit;

namespace FitFileOverlay.Overlay;

public record GPSPoint(double Latitude, double Longitude)
{
    public static GPSPoint FromFitRecord(RecordMesg record)
    {
        int fitLatitude = record.GetPositionLat() ?? 0;
        int fitLongitude = record.GetPositionLong() ?? 0;
        double latitude = fitLatitude * (180d / Int32.MaxValue);
        double longitude = fitLongitude * (180d / Int32.MaxValue);
        return new GPSPoint(latitude, longitude);
    }

    private static (double xDistance, double yDistance) GetDistanceBetweenPointsInMeters(GPSPoint gpsPointA, GPSPoint gpsPointB)
    {
        double latDegToMeter = 111320;
        double earthCumference = 40075000;
        double latDiff = gpsPointA.Latitude - gpsPointB.Latitude;
        double lonDiff = gpsPointA.Longitude - gpsPointB.Longitude;
        //convert degree difference to meters; compensate longitude conversion by multiplying with cosine of latitude
        double x = latDiff * latDegToMeter;
        double y = lonDiff * (earthCumference * Math.Cos(gpsPointA.Latitude * (Math.PI / 180)) / 360);
        return (x, y);
    }
}
