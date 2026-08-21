using Dynastream.Fit;

namespace FitFileOverlay.Overlay;

public record GpsPoint(double Latitude, double Longitude)
{
    public static GpsPoint? FromFitRecord(RecordMesg record)
    {
        if (record.GetPositionLat() is null || record.GetPositionLong() is null)
            return null;
        int fitLatitude = record.GetPositionLat() ?? 0;
        int fitLongitude = record.GetPositionLong() ?? 0;
        double latitude = fitLatitude * (180d / Int32.MaxValue);
        double longitude = fitLongitude * (180d / Int32.MaxValue);
        return new GpsPoint(latitude, longitude);
    }

    /// <summary>
    /// Returns a list of points matching the input points, scaled and transformed so that they represent
    /// coordinates in a unitary screen space (0,0) top-left; (1,1) bottom-right.
    /// The points fit exactly in this space (there will be at least 2 points on the border)
    /// </summary>
    /// <param name="points"></param>
    /// <param name="aspectRatio"></param>
    /// <returns></returns>
    public static List<(double x, double y)?> PointsListToUnitaryScreenSpace(List<GpsPoint?> points, out double aspectRatio)
    {
        List<(double x, double y)?> resultPoints = [];
        //Find extremeties
        int topIndex, bottomIndex, leftIndex, rightIndex;
        topIndex = bottomIndex = leftIndex = rightIndex = 0;
        double top = double.MinValue, bottom = double.MaxValue, left = double.MaxValue, right = double.MinValue;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        for (int i = 0; i < points.Count; ++i)
            if(points[i] != null)
            {
                if (points[i].Latitude > top)
                {
                    top = points[i].Latitude;
                    topIndex = i;
                }
                if (points[i].Latitude < bottom)
                {
                    bottom = points[i].Latitude;
                    bottomIndex = i;
                }
                if (points[i].Longitude > right)
                {
                    right = points[i].Longitude;
                    rightIndex = i;
                }
                if (points[i].Longitude < left)
                {
                    left = points[i].Longitude;
                    leftIndex = i;
                }
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        //Determine the centerpoint and convert GPS points to meters relative to centerpoint
        GpsPoint gpsCenterPoint = new((top + bottom) / 2, (right + left) / 2);
        for (int i = 0; i < points.Count; ++i)
            if (points[i] != null)
#pragma warning disable CS8604 // Possible null reference argument.
                resultPoints.Add(GetDistanceBetweenPointsInMeters(points[i], gpsCenterPoint));
#pragma warning restore CS8604 // Possible null reference argument.
            else
                resultPoints.Add(null);
        //Normalize result points to be between 0 and 1
        double? xSizeMeters = resultPoints[rightIndex]?.x - resultPoints[leftIndex]?.x;
        double? ySizeMeters = resultPoints[topIndex]?.y - resultPoints[bottomIndex]?.y;
        aspectRatio = xSizeMeters / ySizeMeters ?? 1;
        double scale;
        if (aspectRatio < 1)
        {
            //The path defined by the points is taller than it is wide
            scale = 1 / ySizeMeters ?? 1;
        }
        else
        {
            //The path defined by the points is wider than it is tall
            scale = 1 / xSizeMeters ?? 1;
        }
        for (int i = 0; i < resultPoints.Count; ++i)
            if(resultPoints[i] != null)
            {
                //scale and invert y axis to match screen space: top-left corner is (0, 0), right-bottom is (1, 1)
                double newX = (resultPoints[i]?.x ?? 1)  * scale;
                double newY = (resultPoints[i]?.y ?? 1) * (-scale);
                //offset so that center is at (0.5, 0.5)
                newX += 0.5;
                newY += 0.5;
                resultPoints[i] = (newX, newY);
            }
        return resultPoints;
    }

    private static (double xDistance, double yDistance) GetDistanceBetweenPointsInMeters(GpsPoint gpsPointA, GpsPoint gpsPointB)
    {
        double latDegToMeter = 111320;
        double earthCumference = 40075000;
        double latDiff = gpsPointA.Latitude - gpsPointB.Latitude;
        double lonDiff = gpsPointA.Longitude - gpsPointB.Longitude;
        //convert degree difference to meters; compensate longitude conversion by multiplying with cosine of latitude
        double y = latDiff * latDegToMeter;
        double x = lonDiff * (earthCumference * Math.Cos(gpsPointA.Latitude * (Math.PI / 180)) / 360);
        return (x, y);
    }
}
