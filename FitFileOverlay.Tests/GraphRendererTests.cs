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
