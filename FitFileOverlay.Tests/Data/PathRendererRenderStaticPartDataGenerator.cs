using FitFileOverlay.Helpers;
using SkiaSharp;

namespace FitFileOverlay.Tests.Data;

public class PathRendererRenderStaticPartDataGenerator : DataSourceGeneratorAttribute<PathRendererRenderStaticPartTestData>
{
    protected override IEnumerable<Func<PathRendererRenderStaticPartTestData>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
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
        yield return () => new PathRendererRenderStaticPartTestData(rendererOptions, points, "Test1.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new PathRendererRenderStaticPartTestData(rendererOptions, points, "Test2_transparency.png");
    }
}

public record PathRendererRenderStaticPartTestData(PathRendererOptions RendererOptions, List<SKPoint?> Points, string FileName);
