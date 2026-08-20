using FitFileOverlay.Overlay;

using SkiaSharp;

using static FitFileOverlay.Overlay.DataFieldRenderer;

namespace FitFileOverlay.Tests;

public class DataFieldRendererTests
{
    [Test]
    [MethodDataSource(typeof(DataFieldRendererTestDataSources), nameof(DataFieldRendererTestDataSources.DataFieldRendererTestData))]
    public async Task Render_ValidInput_ExpectedResult(DataFieldRendererTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = DataFieldRenderer.Render(testData.RendererOptions, testData.Label, testData.Value, testData.Unit);

        //Assert
        await Assert.That(result).IsNotNull();
        //save to file to check manually if it's correct
        TrySaveImageToFile(result, testData.FileName);
    }

    private static void TrySaveImageToFile(SKBitmap bitmap, string fileName)
    {
        string? directoryName = Path.GetDirectoryName(fileName);
        if (directoryName != null)
        {
            Directory.CreateDirectory(directoryName);
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, quality: 80);
            using Stream stream = File.Open(fileName, FileMode.Create);
            data.SaveTo(stream);
        }
    }
}

public record DataFieldRendererTestData(DataFieldRendererOptions RendererOptions, string Label, string Value, string Unit, string FileName);

public static class DataFieldRendererTestDataSources
{
    public static IEnumerable<Func<DataFieldRendererTestData>> DataFieldRendererTestData()
    {
        DataFieldRendererOptions rendererOptions = new()
        {
            BitmapHeight = 400,
            BitmapWidth = 400,
            Background = SKColors.Transparent,
            LabelColor = SKColors.White,
            LabelFont = new SKFont(SKTypeface.FromFamilyName("Arial"), size: 48),
            ValueColor = SKColors.White,
            ValueFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 96),
            UnitColor = SKColors.Orange,
            UnitFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 48),
            LineSpacing = 20
        };
        yield return () => new DataFieldRendererTestData(rendererOptions, "Pace", "4:30", "/KM", "./DataFieldTestOutput/DataFieldTest1.png");

        rendererOptions.ValueColor = new SKColor(249, 137, 37);
        yield return () => new DataFieldRendererTestData(rendererOptions, "HeartRate", "187", "BPM", "./DataFieldTestOutput/DataFieldTest2.png");

        rendererOptions.ValueColor = SKColors.White;
        rendererOptions.Background = SKColors.DarkCyan;
        yield return () => new DataFieldRendererTestData(rendererOptions, "Distance", "4.21", "KM", "./DataFieldTestOutput/DataFieldTest3.png");

        rendererOptions.ValueFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 48);
        yield return () => new DataFieldRendererTestData(rendererOptions, "Timestamp", "09-Aug-26 10:12:56", string.Empty, "./DataFieldTestOutput/DataFieldTest4.png");
    }
}
