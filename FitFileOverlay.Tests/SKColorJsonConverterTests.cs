using FitFileOverlay.Helpers;
using SkiaSharp;
using System.Text;
using System.Text.Json;

namespace FitFileOverlay.Tests;

public class SKColorJsonConverterTests
{
    [Test]
    [InstanceMethodDataSource(nameof(TestData))]
    public async Task Read_ConvertsJsonStringToSKColor(string json, SKColor expected)
    {
        // Arrange
        SKColorJsonConverter sut = new();
        JsonSerializerOptions options = new();
        options.Converters.Add(sut);
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
        reader.Read(); // Move to the first token

        // Act
        SKColor result = sut.Read(ref reader, typeof(SkiaSharp.SKColor), options);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [InstanceMethodDataSource(nameof(TestData))]
    public async Task Write_ConvertsSKColorToJsonString(string expectedJson, SKColor color)
    {
        // Arrange
        SKColorJsonConverter sut = new();
        JsonSerializerOptions options = new();
        options.Converters.Add(sut);
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);

        // Act
        sut.Write(writer, color, options);
        await writer.FlushAsync();
        string resultJson = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        await Assert.That(resultJson).IsEqualTo(expectedJson);
    }

    private static IEnumerable<(string json, SKColor expected)> TestData => [
            ("\"#ffff0000\"", SKColors.Red),
            ("\"#00ffffff\"", SKColors.Transparent) ];
}
