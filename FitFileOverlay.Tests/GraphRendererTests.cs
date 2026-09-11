using FitFileOverlay.Helpers;
using FitFileOverlay.Tests.Data;
using SkiaSharp;

namespace FitFileOverlay.Tests;

public class GraphRendererTests
{
    [Test]
    [GraphRendererRenderStaticPartDataGenerator]
    public async Task RenderStaticPartTest(GraphRendererRenderStaticPartTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = GraphRenderer.RenderStaticPart(testData.RendererOptions, testData.Values);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "TestOutput", "GraphRenderer_RenderStaticPart", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Test]
    [GraphRendererRenderTrailPartDataGenerator]
    public async Task RenderTrailPart_ValidInput_ExpectedResult(GraphRendererRenderTrailPartTestData testData)
    {
        //Arrange

        //Act
        SKBitmap? pathCache = null;
        SKBitmap result = GraphRenderer.RenderTrailPart(testData.RendererOptions, testData.Values, testData.CurrentValueIndex, (x) => x, ref pathCache);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "TestOutput", "GraphRenderer_RenderTrailPart", (testData.FileName ?? string.Empty));
        SaveImageToFile(result, fileName);
        TestContext.Current!.Output.AttachArtifact(fileName);
    }

    [Test]
    [GraphRendererRenderTrailPartDataGenerator]
    public async Task RenderTrailPart_ValidInputWithPathCache_ExpectedResult(GraphRendererRenderTrailPartTestData testData)
    {
        //Arrange

        //Act
        SKBitmap? pathCache = null;
        _ = GraphRenderer.RenderTrailPart(testData.RendererOptions, testData.Values, testData.CurrentValueIndex - 1, (x) => x, ref pathCache);
        SKBitmap result = GraphRenderer.RenderTrailPart(testData.RendererOptions, testData.Values, testData.CurrentValueIndex, (x) => x, ref pathCache);

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
