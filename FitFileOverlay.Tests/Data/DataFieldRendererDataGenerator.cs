using FitFileOverlay.Helpers;
using SkiaSharp;

namespace FitFileOverlay.Tests.Data;

public class DataFieldRendererDataGenerator : DataSourceGeneratorAttribute<DataFieldRendererTestData>
{
    protected override IEnumerable<Func<DataFieldRendererTestData>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        DataFieldRendererOptions rendererOptions = new()
        {
            BitmapHeight = 400,
            BitmapWidth = 400,
            LabelColor = SKColors.White,
            LabelFont = new SKFont(SKTypeface.FromFamilyName("Arial"), size: 48),
            ValueColor = SKColors.White,
            ValueFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 96),
            UnitColor = SKColors.Orange,
            UnitFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 48),
            LineSpacing = 20
        };
        yield return () => new DataFieldRendererTestData(rendererOptions, "Pace", "4:30", "/KM", "Test1.png");

        rendererOptions.ValueColor = new SKColor(249, 137, 37);
        yield return () => new DataFieldRendererTestData(rendererOptions, "HeartRate", "187", "BPM", "Test2.png");

        rendererOptions.ValueColor = SKColors.White;
        yield return () => new DataFieldRendererTestData(rendererOptions, "Distance", "4.21", "KM", "Test3.png");

        rendererOptions.ValueFont = new SKFont(SKTypeface.FromFamilyName("Impact", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic), 48);
        yield return () => new DataFieldRendererTestData(rendererOptions, "Timestamp", "09-Aug-26 10:12:56", string.Empty, "Test4.png");
    }
}

public record DataFieldRendererTestData(DataFieldRendererOptions RendererOptions, string Label, string Value, string Unit, string FileName);
