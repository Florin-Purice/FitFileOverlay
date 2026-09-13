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
            150f,
            200f
        ];
        List<float?> xPos =
        [
            0f,
            0.1f,
            0.2f,
            0.3f,
            0.4f,
            0.5f,
            0.6f,
            0.7f,
            0.8f,
            0.9f,
            1f,
        ];

        yield return () => new GraphRendererRenderStaticPartTestData(rendererOptions, values, xPos, "Test1.png");
    }
}

public record GraphRendererRenderStaticPartTestData(GraphRendererOptions RendererOptions, List<float?> Values, List<float?> XPos, string FileName);
