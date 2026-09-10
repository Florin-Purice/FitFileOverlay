using FitFileOverlay.Helpers;
using SkiaSharp;

namespace FitFileOverlay.Tests.Data;

public class GraphRendererRenderTrailPartDataGenerator : DataSourceGeneratorAttribute<GraphRendererRenderTrailPartTestData>
{
    protected override IEnumerable<Func<GraphRendererRenderTrailPartTestData>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        GraphRendererOptions rendererOptions = new()
        {
            BitmapHeight = 200,
            BitmapWidth = 1000,
            PrimaryColor = SKColors.White,
            SecondaryColor = SKColors.Orange,
            TertiaryColor = new SKColor(100, 100, 100, 100),
            BackgroundColor = new SKColor(100, 100, 100, 50),
            BottomPaddingPercent = 0.1f,
            MinimumRange = 10f,
            StrokeWidth = 6,
            FadePointCount = 5
        };
        List<float?> values =
        [
            100f,
            150f,
            180f,
            200f,
            120f,
            90f,
            100f,
            105f,
            120f,
            150f
        ];
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, 8, "Test1.png");
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, 4, "Test2.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, 9, "Test3_transparency.png");
    }
}

public record GraphRendererRenderTrailPartTestData(GraphRendererOptions RendererOptions, List<float?> Values, int CurrentValueIndex, string FileName);
