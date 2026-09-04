using FitFileOverlay.Helpers;
using SkiaSharp;
using System.Windows.Media;

namespace FitFileOverlay.Tests;

public class SKColorToColorConverterTests
{
    [Test]
    [Arguments(255, 0, 0, 0)]
    [Arguments(100, 50, 200, 10)]
    public async Task Convert_ConvertsSKColorToColor(byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        SKColorToColorConverter sut = new();
        SKColor skColor = new(red, green, blue, alpha);

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(skColor, typeof(Color), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        Color color = await Assert.That(result).IsTypeOf<Color>();
        await Assert.That(color.A).IsEqualTo(alpha);
        await Assert.That(color.R).IsEqualTo(red);
        await Assert.That(color.G).IsEqualTo(green);
        await Assert.That(color.B).IsEqualTo(blue);
    }

    [Test]
    public async Task Convert_ThrowsArgumentException_WhenValueIsNotSKColor()
    {
        // Arrange
        SKColorToColorConverter sut = new();
        object notSKColor = new();

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.Convert(notSKColor, typeof(Color), null, null)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Test]
    [Arguments(255, 0, 0, 0)]
    [Arguments(100, 50, 200, 10)]
    public async Task ConvertBack_ConvertsColorToSKColor(byte alpha, byte red, byte green, byte blue)
    {
        // Arrange
        SKColorToColorConverter sut = new();
        Color color = Color.FromArgb(alpha, red, green, blue);

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.ConvertBack(color, typeof(SKColor), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        SKColor skColor = await Assert.That(result).IsTypeOf<SKColor>();
        await Assert.That(skColor.Alpha).IsEqualTo(alpha);
        await Assert.That(skColor.Red).IsEqualTo(red);
        await Assert.That(skColor.Green).IsEqualTo(green);
        await Assert.That(skColor.Blue).IsEqualTo(blue);
    }

    [Test]
    public async Task ConvertBack_ThrowsArgumentException_WhenValueIsNotColor()
    {
        // Arrange
        SKColorToColorConverter sut = new();
        object notColor = new();

        // Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        await Assert.That(() => sut.ConvertBack(notColor, typeof(SKColor), null, null)).Throws<ArgumentException>();
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}
