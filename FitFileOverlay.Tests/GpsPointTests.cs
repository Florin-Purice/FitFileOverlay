using FitFileOverlay.Models;
using FitFileOverlay.Tests.Data;

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
