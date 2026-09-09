using FitFileOverlay.Converters;
using FitFileOverlay.Enums;

namespace FitFileOverlay.Tests;

public class EnumDescriptionTypeConverterTests
{
    [Test]
    [Arguments(SpeedUnit.KilometersPerHour, "km/h")]
    [Arguments(DataFieldType.StrideLength, "Stride Length")]
    [Arguments(PaceUnit.MinutesPerKilometer, "min/km")]
    public async Task ConvertTo_ReturnsDescription_WhenEnumHasDescriptionAttribute(Enum enumVal, string expectedDescription)
    {
        // Arrange
        EnumDescriptionTypeConverter converter = new(enumVal.GetType());

        // Act
        object? result = converter.ConvertTo(enumVal, typeof(string));

        // Assert
        await Assert.That(result).IsNotNull();
        string? description = await Assert.That(result).IsTypeOf<string>();
        await Assert.That(description).IsEqualTo(expectedDescription);
    }
}
