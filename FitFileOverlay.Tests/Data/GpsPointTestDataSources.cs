using FitFileOverlay.Models;

namespace FitFileOverlay.Tests.Data;

public static class GpsPointTestDataSources
{
    public static IEnumerable<GpsPointPointsListToUnitaryScreenSpaceTestData> PointsListToUnitaryScreenSpaceTestData()
    {
        List<GpsPoint?> points = [
            new GpsPoint(10, 5),
            new GpsPoint(12, 20),
            new GpsPoint(30, 15),
            new GpsPoint(40, 25)];
        yield return new GpsPointPointsListToUnitaryScreenSpaceTestData(points);
        
        points = [
            new GpsPoint(-20, 5),
            new GpsPoint(0, 0),
            new GpsPoint(30, 15),
            new GpsPoint(60, -25)];
        yield return new GpsPointPointsListToUnitaryScreenSpaceTestData(points);
    }
}

public record GpsPointPointsListToUnitaryScreenSpaceTestData(List<GpsPoint?> Points);