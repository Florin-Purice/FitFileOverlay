using FitFileOverlay.Converters;
using SkiaSharp;
using System.Windows.Media;

namespace FitFileOverlay.Tests;

public class AlphaToColorConverterTests
{
    [Test]
    [Arguments(0)]
    [Arguments(100)]
    public async Task Convert_ConvertsAlphaToColor(byte alpha)
    {
        // Arange
        AlphaToColorConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(alpha, typeof(Color), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Color color = await Assert.That(result).IsTypeOf<Color>();
        await Assert.That(color.A).IsEqualTo(alpha);
    }

    [Test]
    public async Task Convert_ThrowsArgumentException_WhenValueIsNotByte()
    {
        // Arange
        AlphaToColorConverter sut = new();
        string notByte = "not a byte";

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.Convert(notByte, typeof(Color), null, null)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Test]
    [Arguments(255, 0, 0, 0)]
    [Arguments(100, 50, 200, 10)]
    public async Task ConvertBack_ConvertsColorToAlpha(byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        AlphaToColorConverter sut = new();
        Color color = Color.FromArgb(alpha, red, green, blue);

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.ConvertBack(color, typeof(byte), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        byte resultAlpha = await Assert.That(result).IsTypeOf<byte>();
        await Assert.That(resultAlpha).IsEqualTo(alpha);
    }

    [Test]
    public async Task ConvertBack_ThrowsArgumentException_WhenValueIsNotColor()
    {
        // Arrange
        AlphaToColorConverter sut = new();
        object notColor = new();

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.ConvertBack(notColor, typeof(byte), null, null)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
