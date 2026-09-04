using FitFileOverlay.Helpers;
using SkiaSharp;

namespace FitFileOverlay.Tests.Data;

public class PathRendererRenderUntilPointDataGenerator : DataSourceGeneratorAttribute<PathRendererRenderUntilPointTestData>
{
    protected override IEnumerable<Func<PathRendererRenderUntilPointTestData>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
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
        yield return () => new PathRendererRenderUntilPointTestData(rendererOptions, points, 8, "Test1.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new PathRendererRenderUntilPointTestData(rendererOptions, points, 9, "Test2_transparency.png");
    }
}

public record PathRendererRenderUntilPointTestData(PathRendererOptions RendererOptions, List<SKPoint?> Points, int CurrentPointIndex, string FileName);
