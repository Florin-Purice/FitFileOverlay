using FitFileOverlay.Overlay;

namespace FitFileOverlay.Tests;

public class GpsPointTests
{
    [Test]
    [MethodDataSource(typeof(GpsPointTestDataSources), nameof(GpsPointTestDataSources.PointsListToUnitaryScreenSpaceTestData))]
    public async Task PointsListToUnitaryScreenSpace_ValidInput_IsInsideExpectedBounds(GpsPointPointsListToUnitaryScreenSpaceTestData testData)
    {
        //Arrange

        //Act
        List<(double x, double y)?> result = GpsPoint.PointsListToUnitaryScreenSpace(testData.Points, out double resultAR);

        //Assert
        await Assert.That(result).Count().IsEqualTo(testData.Points.Count);
        await Assert.That(result).All(v => v?.x >= 0 && v?.x <= 1 && v?.y >= 0 && v?.y <= 1);
    }
}

public record GpsPointPointsListToUnitaryScreenSpaceTestData(List<GpsPoint?> Points);

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
    }
}
