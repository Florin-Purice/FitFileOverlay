using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;

namespace FitFileOverlay.Tests;

public class DataFieldTypeToStringConverterTests
{
    [Test]
    [Arguments(DataFieldType.Pace, "Pace")]
    [Arguments(DataFieldType.Speed, "Speed")]
    [Arguments(DataFieldType.Cadence, "Cadence")]
    public async Task Convert_ReturnsCorrectString_WhenGivenValidDataFieldType(DataFieldType input, string expected)
    {
        // Arrange
        DataFieldTypeToStringConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(input, typeof(string), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Convert_ReturnsEmptyString_WhenGivenInvalidInput()
    {
        // Arrange
        DataFieldTypeToStringConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(default, typeof(string), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    [Arguments("Pace", DataFieldType.Pace)]
    [Arguments("Speed", DataFieldType.Speed)]
    [Arguments("Cadence", DataFieldType.Cadence)]
    public async Task ConvertBack_ReturnsCorrectDataFieldType_WhenGivenValidString(string input, DataFieldType expected)
    {
        // Arrange
        DataFieldTypeToStringConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.ConvertBack(input, typeof(DataFieldType), default, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ConvertBack_ThrowsArgumentException_WhenGivenInvalidString()
    {
        // Arrange
        DataFieldTypeToStringConverter sut = new();
        string invalidInput = "InvalidString";

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.ConvertBack(invalidInput, typeof(DataFieldType), default, default)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
