using FitFileOverlay.Converters;
using SkiaSharp;
using System.Text;
using System.Text.Json;

namespace FitFileOverlay.Tests;

public class TimeSpanToStringConverterTests
{
    [Test]
    public async Task Convert_ReturnsEmptyString_WhenValueIsNotTimeSpan()
    {
        // Arrange
        TimeSpanToStringConverter sut = new();
        int notATimeSpan = 5;

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(notATimeSpan, typeof(string), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        string? resultString = await Assert.That(result).IsNotNull().And.IsTypeOf<string>();
        await Assert.That(resultString).IsEqualTo(string.Empty);
    }

    [Test]
    [InstanceMethodDataSource(nameof(TestData))]
    public async Task Convert_ReturnsCorrectString(TimeSpan value, string expected)
    {
        // Arrange
        TimeSpanToStringConverter sut = new();

        // Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        object result = sut.Convert(value, typeof(string), null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // Assert
        string? resultString = await Assert.That(result).IsNotNull().And.IsTypeOf<string>();
        await Assert.That(resultString).IsEqualTo(expected);
    }

    private static IEnumerable<(TimeSpan value, string expected)> TestData => [
            (TimeSpan.FromSeconds(3600 + 120 + 34), "1:02:34"),
            (TimeSpan.FromSeconds(120 + 34), "02:34") ];
}
