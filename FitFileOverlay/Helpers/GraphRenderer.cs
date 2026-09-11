using FitFileOverlay.Models;
using SkiaSharp;
using System.Windows.Controls;

namespace FitFileOverlay.Helpers;

public class GraphRenderer
{
    private const float _textMargin = 10f;

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
        float minPixels = (100f-options.BottomPaddingPercent)/100f * options.BitmapHeight;
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
            Color = new SKColor(options.TertiaryColor.Red, options.TertiaryColor.Green, options.TertiaryColor.Blue, options.BackgroundAlpha),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawPath(graphBackgroundPath, paint);
        paint.Style = SKPaintStyle.Stroke;
        paint.Color = options.TertiaryColor;
        paint.StrokeWidth = options.StrokeWidth;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.PathEffect = SKPathEffect.CreateCorner(options.StrokeWidth);
        canvas.DrawPath(trail, paint);

        return bitmap;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="options"></param>
    /// <param name="values"></param>
    /// <param name="currentValueIndex"></param>
    /// <param name="valueConverter">A function that converts the given values according to the specified unit</param>
    /// <param name="previousTrailBase"></param>
    /// <returns></returns>
    public static SKBitmap RenderTrailPart(GraphRendererOptions options, List<float?> values, int currentValueIndex, Func<float?, float?> valueConverter, ref SKBitmap? previousTrailBase)
    {
        float topPadding = 2f * (float)options.StrokeWidth;
        float min = values.Min() ?? 0f;
        float max = values.Max() ?? 1f;
        float range = max - min;
        if (range < options.MinimumRange)
            range = options.MinimumRange;
        float maxPixels = topPadding;
        float minPixels = (100f - options.BottomPaddingPercent)/100f * options.BitmapHeight;
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

        if (values[currentValueIndex] != null)
        {
            // Calculate positions
            float x = (float)currentValueIndex / (values.Count - 1) * options.BitmapWidth;
            float y = valueToPixel(values[currentValueIndex] ?? 0f);
            string valueText = $"{valueConverter(values[currentValueIndex]):0}";
            options.ValueFont.MeasureText(valueText, out SKRect valueSize, skPaint);
            options.UnitFont.MeasureText(options.UnitText, out SKRect unitSize, skPaint);
            float textWidth = valueSize.Width + unitSize.Width;
            float textHeight = Math.Max(valueSize.Height, unitSize.Height);
            float textBottom = options.BitmapHeight - _textMargin;
            float textTop = textBottom - textHeight;
            float textLeft = x - textWidth / 2f;
            // Ensure text is within bitmap bounds
            if (textLeft < 0)
                textLeft = 0;
            else if (textLeft + textWidth > options.BitmapWidth)
                textLeft = options.BitmapWidth - textWidth;
            if (options.IsTextEnabled)
            {
                // Draw projection line from top of text up to current position
                // Layer under positon marker
                float lineToY = textTop - _textMargin;
                if(lineToY > y)
                {
                    SKPathBuilder pathBuilder = new();
                    pathBuilder.MoveTo(x, textTop - _textMargin);
                    pathBuilder.LineTo(x, y);
                    skPaint.PathEffect = SKPathEffect.CreateDash([8, 8], 0);
                    skPaint.Style = SKPaintStyle.Stroke;
                    skPaint.StrokeWidth = options.StrokeWidth / 2f;
                    skPaint.StrokeCap = SKStrokeCap.Round;
                    skPaint.Color = options.SecondaryColor;
                    canvas.DrawPath(pathBuilder.Detach(), skPaint);
                }
            }
            // Mark current position with a circle
            skPaint.Color = options.SecondaryColor;
            skPaint.Style = SKPaintStyle.Fill;
            canvas.DrawCircle(x, y, options.StrokeWidth * 2, skPaint);
            skPaint.Color = options.PrimaryColor;
            skPaint.StrokeWidth = options.StrokeWidth / 2f;
            skPaint.Style = SKPaintStyle.Stroke;
            skPaint.PathEffect = null;
            canvas.DrawCircle(x, y, options.StrokeWidth * 2, skPaint);
            if (options.IsTextEnabled)
            {
                // Draw current value text
                //draw value
                skPaint.Style = SKPaintStyle.Fill;
                skPaint.Color = options.PrimaryColor;
                canvas.DrawText(valueText, textLeft, textBottom, SKTextAlign.Left, options.ValueFont, skPaint);
                // draw unit
                skPaint.Color = options.SecondaryColor;
                unitSize.Offset(valueSize.Right, valueSize.Bottom);
                canvas.DrawText(options.UnitText, textLeft + valueSize.Width, textBottom, SKTextAlign.Left, options.UnitFont, skPaint);
            }
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
    public byte BackgroundAlpha { get; set; }
    public SKFont ValueFont { get; set; }
    public SKFont UnitFont { get; set; }
    public float BottomPaddingPercent { get; set; }
    public float MinimumRange { get; set; }
    public float StrokeWidth { get; set; }
    public int FadePointCount { get; set; }
    public string UnitText { get; set; }
    public bool IsTextEnabled { get; set; }
}
