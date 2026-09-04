using FitFileOverlay.Helpers;
using Wpf.Ui.Appearance;

namespace FitFileOverlay.Tests;

public class EnumToBooleanConverterTests
{
    [Test]
    public async Task Convert_ReturnsTrue_WhenValueMatchesParameter()
    {
        // Arrange
        EnumToBooleanConverter sut = new();
        ApplicationTheme value = ApplicationTheme.Light;
        string parameter = "Light";

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(value, typeof(bool), parameter, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That((bool)result).IsTrue();
    }

    [Test]
    public async Task Convert_ReturnsFalse_WhenValueDoesNotMatchParameter()
    {
        // Arrange
        EnumToBooleanConverter sut = new();
        ApplicationTheme value = ApplicationTheme.Dark;
        string parameter = "Light";

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(value, typeof(bool), parameter, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        await Assert.That((bool)result).IsFalse();
    }

    [Test]
    public async Task Convert_ThrowsArgumentException_WhenInputIsInvalid()
    {
        // Arrange
        EnumToBooleanConverter sut = new();
        object value = new();
        object parameter = new();

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.Convert(value, typeof(bool), parameter, null)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
