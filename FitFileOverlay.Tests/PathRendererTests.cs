using FitFileOverlay.Overlay;

using SkiaSharp;

using static FitFileOverlay.Overlay.PathRenderer;

namespace FitFileOverlay.Tests;

internal class PathRendererTests
{
    [Test]
    [MethodDataSource(typeof(PathRendererTestDataSources), nameof(PathRendererTestDataSources.RenderFullTestData))]
    public async Task RenderFull_ValidInput_ExpectedResult(PathRendererRenderFullTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = PathRenderer.RenderFull(testData.RendererOptions, testData.Points);

        //Assert
        await Assert.That(result).IsNotNull();
        //save to file to check manually if it's correct
        TrySaveImageToFile(result, testData.FileName);
    }

    [Test]
    [MethodDataSource(typeof(PathRendererTestDataSources), nameof(PathRendererTestDataSources.RenderUntilPointTestData))]
    public async Task RenderUntilPoint_ValidInput_ExpectedResult(PathRendererRenderUntilPointTestData testData)
    {
        //Arrange

        //Act
        SKBitmap result = PathRenderer.RenderUntilPoint(testData.RendererOptions, testData.Points, testData.CurrentPointIndex);

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

public record PathRendererRenderFullTestData(PathRendererOptions RendererOptions, List<SKPoint?> Points, string FileName);

public record PathRendererRenderUntilPointTestData(PathRendererOptions RendererOptions, List<SKPoint?> Points, int CurrentPointIndex, string FileName);

public static class PathRendererTestDataSources
{
    public static IEnumerable<Func<PathRendererRenderFullTestData>> RenderFullTestData()
    {
        PathRendererOptions rendererOptions = new()
        {
            BitmapHeight = 400,
            BitmapWidth = 400,
            PrimaryColor = SKColors.White,
            StrokeWidth = 6
        };
        List<SKPoint?> points = 
        [
            new SKPoint(100, 100),
            new SKPoint(200, 100),
            new SKPoint(300, 150),
            new SKPoint(300, 200),
            new SKPoint(300, 250),
            new SKPoint(300, 300),
            new SKPoint(300, 320),
            new SKPoint(330, 300),
            new SKPoint(360, 390),
            new SKPoint(300, 100)
        ];
        yield return () => new PathRendererRenderFullTestData(rendererOptions, points, "./PathRenderTestOutput/RenderFullTest1.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new PathRendererRenderFullTestData(rendererOptions, points, "./PathRenderTestOutput/RenderFullTest2_transparency.png");
    }

    public static IEnumerable<Func<PathRendererRenderUntilPointTestData>> RenderUntilPointTestData()
    {
        PathRendererOptions rendererOptions = new()
        {
            BitmapHeight = 400,
            BitmapWidth = 400,
            PrimaryColor = SKColors.White,
            SecondaryColor = SKColors.Orange,
            StrokeWidth = 6,
            FadePointCount = 6
        };
        List<SKPoint?> points =
        [
            new SKPoint(100, 100),
            new SKPoint(200, 100),
            new SKPoint(300, 150),
            new SKPoint(300, 200),
            new SKPoint(300, 250),
            new SKPoint(300, 300),
            new SKPoint(300, 320),
            new SKPoint(330, 300),
            new SKPoint(360, 390),
            new SKPoint(300, 100)
        ];
        yield return () => new PathRendererRenderUntilPointTestData(rendererOptions, points, 8, "./PathRenderTestOutput/RenderUntilPointTest1.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new PathRendererRenderUntilPointTestData(rendererOptions, points, 9, "./PathRenderTestOutput/RenderUntilPointTest2_transparency.png");
    }
}