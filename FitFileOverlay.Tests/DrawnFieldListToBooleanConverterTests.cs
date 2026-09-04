using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;

namespace FitFileOverlay.Tests;

public class DrawnFieldListToBooleanConverterTests
{
    [Test]
    public async Task Convert_ReturnsTrue_WhenFieldIsInList()
    {
        // Arrange
        DrawnFieldListToBooleanConverter sut = new();
        List<DataFieldType> drawnDataFields = [DataFieldType.HeartRate, DataFieldType.Speed];
        DataFieldType fieldToCheck = DataFieldType.HeartRate;

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(drawnDataFields, typeof(bool), fieldToCheck, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That((bool)result).IsTrue();
    }

    [Test]
    public async Task Convert_ReturnsFalse_WhenFieldIsNotInList()
    {
        // Arrange
        DrawnFieldListToBooleanConverter sut = new();
        List<DataFieldType> drawnDataFields = [DataFieldType.HeartRate, DataFieldType.Speed];
        DataFieldType fieldToCheck = DataFieldType.Cadence;

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(drawnDataFields, typeof(bool), fieldToCheck, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That((bool)result).IsFalse();
    }

    [Test]
    public async Task Convert_ReturnsFalse_WhenInputIsInvalid()
    {
        // Arrange
        DrawnFieldListToBooleanConverter sut = new();
        object notACollection = new();
        object notADataFieldType = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(notACollection, typeof(bool), notADataFieldType, default);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That((bool)result).IsFalse();
    }
}
