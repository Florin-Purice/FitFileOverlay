using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Helpers;

public class GraphRenderer
{
    /// <summary>
    /// The same for all frames, so it can be rendered once and reused.
    /// Is the base layer of the graph overlay.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    public static SKBitmap RenderStaticPart(GraphRendererOptions options, List<float?> values)
    {
        float topPadding = 2f * (float)options.StrokeWidth;
        float min = values.Min() ?? 0f;
        float max = values.Max() ?? 1f;
        float range = max - min;
        if (range < options.MinimumRange)
            range = options.MinimumRange;
        float maxPixels = topPadding;
        float minPixels = (1f-options.BottomPaddingPercent) * options.BitmapHeight;
        float scalePixels = (maxPixels - minPixels) / range;
        float valueToPixel(float v) => minPixels + (v - min) * scalePixels;

        // Create full path
        using SKPathBuilder pathBuilder = new();
        pathBuilder.MoveTo(0, valueToPixel(values[0] ?? 0f));
        for (int i = 1; i < values.Count; ++i)
        {
            float x = (float)i / (values.Count - 1) * options.BitmapWidth;
            float y = valueToPixel(values[i] ?? 0f);
            pathBuilder.LineTo(x, y);
        }
        // Create the path for the trail
        using SKPath trail = pathBuilder.Snapshot();
        // Close the path to the bottom of the bitmap
        pathBuilder.LineTo(options.BitmapWidth, options.BitmapHeight);
        pathBuilder.LineTo(0, options.BitmapHeight);
        pathBuilder.Close();
        using SKPath graphBackgroundPath = pathBuilder.Detach();
        // Create the bitmap and draw the graph background
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        SKPaint paint = new()
        {
            IsAntialias = true,
            BlendMode = SKBlendMode.Src,
            Color = options.BackgroundColor,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPath(graphBackgroundPath, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = options.TertiaryColor;
        paint.StrokeWidth = options.StrokeWidth;
        canvas.DrawPath(trail, paint);

        return bitmap;
    }

    public static SKBitmap RenderTrailPart(GraphRendererOptions options, List<float?> values, int currentRecordIndex, ref SKBitmap? previousTrailBase)
    {
        throw new NotImplementedException();
    }
}

public struct GraphRendererOptions
{
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public SKColor PrimaryColor { get; set; }
    public SKColor SecondaryColor { get; set; }
    public SKColor TertiaryColor { get; set; }
    public SKColor BackgroundColor { get; set; }
    public SKFont ValueFont { get; set; }
    public SKFont UnitFont { get; set; }
    public float LineSpacing { get; set; }
    public float BottomPaddingPercent { get; set; }
    public float MinimumRange { get; set; }
    public float StrokeWidth { get; set; }
    public int FadePointCount { get; set; }
}
