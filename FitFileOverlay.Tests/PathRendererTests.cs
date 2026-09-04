using FitFileOverlay.Helpers;
using FitFileOverlay.Tests.Data;
using SkiaSharp;

namespace FitFileOverlay.Tests;

public class PathRendererTests
{
    [Test]
    [PathRendererRenderFullDataGenerator]
    public async Task RenderFullTest(PathRendererRenderFullTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = PathRenderer.RenderFull(testData.RendererOptions, testData.Points);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "PathRenderer_RenderFull", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Test]
    [PathRendererRenderUntilPointDataGenerator]
    public async Task RenderUntilPoint_ValidInput_ExpectedResult(PathRendererRenderUntilPointTestData testData)
    {
        //Arrange

        //Act
        SKBitmap? pathCache = null;
        SKBitmap result = PathRenderer.RenderUntilPoint(testData.RendererOptions, testData.Points, testData.CurrentPointIndex, ref pathCache);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "PathRenderer_RenderUntilPoint", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Test]
    [PathRendererRenderUntilPointDataGenerator]
    public async Task RenderUntilPoint_ValidInputWithPathCache_ExpectedResult(PathRendererRenderUntilPointTestData testData)
    {
        //Arrange

        //Act
        SKBitmap? pathCache = null;
        _ = PathRenderer.RenderUntilPoint(testData.RendererOptions, testData.Points, testData.CurrentPointIndex - 1, ref pathCache);
        SKBitmap result = PathRenderer.RenderUntilPoint(testData.RendererOptions, testData.Points, testData.CurrentPointIndex, ref pathCache);

        //Assert
        await Assert.That(pathCache).IsNotNull();
        await Assert.That(result).IsNotNull();
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