using FitFileOverlay.Helpers;
using FitFileOverlay.Tests.Data;
using SkiaSharp;

namespace FitFileOverlay.Tests;

public class PathRendererTests
{
    [Test]
    [PathRendererRenderStaticPartDataGenerator]
    public async Task RenderStaticPartTest(PathRendererRenderStaticPartTestData testData)
    {
        //Arrange

        //Act
        PathRenderer sut = new(testData.RendererOptions, testData.Points);
        SKBitmap result = sut.RenderStaticPart();

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "TestOutput", "PathRenderer_RenderStaticPart", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Test]
    [PathRendererRenderTrailPartDataGenerator]
    public async Task RenderTrailPart_ValidInput_ExpectedResult(PathRendererRenderTrailPartTestData testData)
    {
        //Arrange

        PathRenderer sut = new(testData.RendererOptions, testData.Points);
        SKBitmap result = sut.RenderTrailPart(testData.CurrentPointIndex);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "TestOutput", "PathRenderer_RenderTrailPart", (testData.FileName ?? string.Empty));
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