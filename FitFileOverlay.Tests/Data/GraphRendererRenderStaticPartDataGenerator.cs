using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Tests.Data;

public class GraphRendererRenderStaticPartDataGenerator : DataSourceGeneratorAttribute<GraphRendererRenderStaticPartTestData>
{
    protected override IEnumerable<Func<GraphRendererRenderStaticPartTestData>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        GraphRendererOptions rendererOptions = new()
        {
            BitmapHeight = 200,
            BitmapWidth = 1000,
            PrimaryColor = SKColors.White,
            SecondaryColor = SKColors.Orange,
            TertiaryColor = new SKColor(100, 100, 100, 100),
            BackgroundAlpha = 50,
            BottomPaddingPercent = 10f,
            MinimumRange = 10f,
            StrokeWidth = 3
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

        yield return () => new GraphRendererRenderStaticPartTestData(rendererOptions, values, "Test1.png");
    }
}

public record GraphRendererRenderStaticPartTestData(GraphRendererOptions RendererOptions, List<float?> Values, string FileName);
