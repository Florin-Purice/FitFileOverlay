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
            BackgroundAlpha = 50,
            BottomPaddingPercent = 20f,
            MinimumRange = 10f,
            StrokeWidth = 3,
            FadePointCount = 5,
            UnitText = "M",
            UnitFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 20),
            ValueFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 32),
            IsTextEnabled = false
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
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, xPos, 8, "Test1.png");
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, xPos, 4, "Test2.png");

        rendererOptions.IsTextEnabled = true;
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, xPos, 5, "Test3_text.png");

        rendererOptions.PrimaryColor = new SKColor(250, 0, 0, 50);
        yield return () => new GraphRendererRenderTrailPartTestData(rendererOptions, values, xPos, 9, "Test4_transparency.png");
    }
}

public record GraphRendererRenderTrailPartTestData(GraphRendererOptions RendererOptions, List<float?> Values, List<float?> XPos, int CurrentValueIndex, string FileName);
