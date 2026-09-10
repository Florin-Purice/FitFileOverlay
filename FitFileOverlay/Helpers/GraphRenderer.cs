using FitFileOverlay.Models;
using SkiaSharp;
using System.Windows.Controls;

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
            Color = options.QuaternaryColor,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPath(graphBackgroundPath, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = options.TertiaryColor;
        paint.StrokeWidth = options.StrokeWidth;
        canvas.DrawPath(trail, paint);

        return bitmap;
    }

    public static SKBitmap RenderTrailPart(GraphRendererOptions options, List<float?> values, int currentValueIndex, ref SKBitmap? previousTrailBase)
    {
        float topPadding = 2f * (float)options.StrokeWidth;
        float min = values.Min() ?? 0f;
        float max = values.Max() ?? 1f;
        float range = max - min;
        if (range < options.MinimumRange)
            range = options.MinimumRange;
        float maxPixels = topPadding;
        float minPixels = (1f - options.BottomPaddingPercent) * options.BitmapHeight;
        float scalePixels = (maxPixels - minPixels) / range;
        float valueToPixel(float v) => minPixels + (v - min) * scalePixels;
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth * 2;
        skPaint.Color = options.PrimaryColor;

        SKBitmap basePathBitmap;
        if (previousTrailBase == null)
        {
            basePathBitmap = new(options.BitmapWidth, options.BitmapHeight);
            SKCanvas baseCanvas = new(basePathBitmap);
            for (int i = 0; i < currentValueIndex - 1 && i < values.Count - 1; ++i)
                if (values[i] != null && values[i + 1] != null)
                {
                    float x0 = (float)i / (values.Count - 1) * options.BitmapWidth;
                    float y0 = valueToPixel(values[i] ?? 0f);
                    float x1 = (float)(i + 1) / (values.Count - 1) * options.BitmapWidth;
                    float y1 = valueToPixel(values[i + 1] ?? 0f);
                    baseCanvas.DrawLine(x0, y0, x1, y1, skPaint);
                    //smooth corners by drawing circles
                    baseCanvas.DrawCircle(x0, y0, options.StrokeWidth, skPaint);
                }
        }
        else if (currentValueIndex > 0)
        {
            basePathBitmap = previousTrailBase;
            SKCanvas baseCanvas = new(basePathBitmap);
            if (values[currentValueIndex] != null && values[currentValueIndex - 1] != null)
            {
                float x0 = (float)(currentValueIndex - 1) / (values.Count - 1) * options.BitmapWidth;
                float y0 = valueToPixel(values[currentValueIndex - 1] ?? 0f);
                float x1 = (float)currentValueIndex / (values.Count - 1) * options.BitmapWidth;
                float y1 = valueToPixel(values[currentValueIndex] ?? 0f);
                baseCanvas.DrawLine(x0, y0, x1, y1, skPaint);
                //smooth corners by drawing circles
                baseCanvas.DrawCircle(x1, y1, options.StrokeWidth, skPaint);
            }
        }
        else basePathBitmap = new(options.BitmapWidth, options.BitmapHeight);
        canvas.DrawBitmap(basePathBitmap, 0, 0, SKSamplingOptions.Default);
        previousTrailBase = basePathBitmap;

        //Draw fading path
        if (currentValueIndex > 0)
        {
            int fadeFrames = Math.Min(currentValueIndex, options.FadePointCount) - 1;
            int f = options.FadePointCount - fadeFrames;
            for (int i = currentValueIndex - fadeFrames; i <= currentValueIndex; ++i)
                if (values[i] != null && values[i - 1] != null)
                {
                    float x0 = (float)(i - 1) / (values.Count - 1) * options.BitmapWidth;
                    float y0 = valueToPixel(values[i - 1] ?? 0f);
                    float x1 = (float)i / (values.Count - 1) * options.BitmapWidth;
                    float y1 = valueToPixel(values[i] ?? 0f);
                    float fadePercent = (float)f++ / options.FadePointCount;
                    skPaint.Color = new SKColor(
                        (byte)((1f - fadePercent) * options.PrimaryColor.Red + fadePercent * options.SecondaryColor.Red),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Green + fadePercent * options.SecondaryColor.Green),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Blue + fadePercent * options.SecondaryColor.Blue),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Alpha + fadePercent * options.SecondaryColor.Alpha));
                    canvas.DrawLine(x0, y0, x1, y1, skPaint);
                    //smooth corners by drawing circles
                    canvas.DrawCircle(x0, y0, options.StrokeWidth, skPaint);
                }
        }

        //Mark current position with a circle
        if (values[currentValueIndex] != null)
        {
            float x = (float)currentValueIndex / (values.Count - 1) * options.BitmapWidth;
            float y = valueToPixel(values[currentValueIndex] ?? 0f);
            skPaint.Color = options.SecondaryColor;
            canvas.DrawCircle(x, y, options.StrokeWidth * 2, skPaint);
        }

        return bitmap;
    }
}

public struct GraphRendererOptions
{
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public SKColor PrimaryColor { get; set; }
    public SKColor SecondaryColor { get; set; }
    public SKColor TertiaryColor { get; set; }
    public SKColor QuaternaryColor { get; set; }
    public SKFont ValueFont { get; set; }
    public SKFont UnitFont { get; set; }
    public float LineSpacing { get; set; }
    public float BottomPaddingPercent { get; set; }
    public float MinimumRange { get; set; }
    public float StrokeWidth { get; set; }
    public int FadePointCount { get; set; }
}
