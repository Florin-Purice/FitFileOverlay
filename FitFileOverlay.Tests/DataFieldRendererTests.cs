using FitFileOverlay.Helpers;
using FitFileOverlay.Tests.Data;
using SkiaSharp;

namespace FitFileOverlay.Tests;

public class DataFieldRendererTests
{
    [Test]
    [DataFieldRendererDataGenerator]
    public async Task Render_ValidInput_ExpectedResult(DataFieldRendererTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = DataFieldRenderer.Render(testData.RendererOptions, testData.Label, testData.Value, testData.Unit);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "DataFieldRenderer", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    private static void SaveImageToFile(SKBitmap bitmap, string fileName)
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