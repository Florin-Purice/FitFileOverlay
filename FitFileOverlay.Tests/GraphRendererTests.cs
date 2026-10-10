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
        GraphRenderer sut = new(testData.RendererOptions, testData.Values, testData.XPos);
        using SKBitmap result = new(testData.RendererOptions.BitmapWidth, testData.RendererOptions.BitmapHeight);
        using (SKCanvas canvas = new(result))
            sut.RenderStaticPart(canvas);

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
        GraphRenderer sut = new(testData.RendererOptions, testData.Values, testData.XPos);
        using SKBitmap result = new(testData.RendererOptions.BitmapWidth, testData.RendererOptions.BitmapHeight);
        using (SKCanvas canvas = new(result))
            sut.RenderTrailPart(canvas, testData.CurrentValueIndex, (x) => x);

        //Assert
        await Assert.That(result).IsNotNull();
        //save result image to file and attach artifact
        string fileName = Path.Combine(TestContext.ResultsDirectory, "TestOutput", "GraphRenderer_RenderTrailPart", (testData.FileName ?? string.Empty));
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
