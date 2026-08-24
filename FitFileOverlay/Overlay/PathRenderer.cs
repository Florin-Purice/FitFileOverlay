using SkiaSharp;

namespace FitFileOverlay.Overlay;

public class PathRenderer
{
    public static SKBitmap RenderFull(PathRendererOptions options, List<SKPoint?> points)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth;
        skPaint.Color = options.PrimaryColor;

        for (int i = 0; i < points.Count - 1; ++i)
            if (points[i] != null && points[i + 1] != null)
            {
                canvas.DrawLine(points[i] ?? new(), points[i + 1] ?? new(), skPaint);
                //smooth corners by drawing circles at points
                canvas.DrawCircle(points[i] ?? new(), options.StrokeWidth / 2f, skPaint);
                //canvas.DrawCircle(points[i + 1] ?? new(), options.StrokeWidth / 2f, skPaint);
            }

        return bitmap;
    }

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="points"></param>
    /// <param name="currentPointIndex"></param>
    /// <param name="previousPathBitmap">A bitmap with the path of all points before current one.</param>
    /// <returns></returns>
    public static SKBitmap RenderUntilPoint(PathRendererOptions options, List<SKPoint?> points, int currentPointIndex, ref SKBitmap? previousPathBitmap)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth * 2;
        skPaint.Color = options.PrimaryColor;

        SKBitmap basePathBitmap;
        if (previousPathBitmap == null)
        {
            basePathBitmap = new(options.BitmapWidth, options.BitmapHeight);
            SKCanvas baseCanvas = new(basePathBitmap);
            for (int i = 0; i < currentPointIndex - 1 && i < points.Count - 1; ++i)
                if (points[i] != null && points[i + 1] != null)
                {
                    baseCanvas.DrawLine(points[i] ?? new(), points[i + 1] ?? new(), skPaint);
                    //smooth corners by drawing circles at points
                    baseCanvas.DrawCircle(points[i] ?? new(), options.StrokeWidth, skPaint);
                }
        }
        else if (currentPointIndex > 0)
        {
            basePathBitmap = previousPathBitmap;
            SKCanvas baseCanvas = new(basePathBitmap);
            if (points[currentPointIndex] != null && points[currentPointIndex - 1] != null)
            {
                baseCanvas.DrawLine(points[currentPointIndex] ?? new(), points[currentPointIndex - 1] ?? new(), skPaint);
                //smooth corners by drawing circles at points
                baseCanvas.DrawCircle(points[currentPointIndex] ?? new(), options.StrokeWidth, skPaint);
            }
        }
        else basePathBitmap = new(options.BitmapWidth, options.BitmapHeight);
        canvas.DrawBitmap(basePathBitmap, 0, 0, SKSamplingOptions.Default);
        previousPathBitmap = basePathBitmap;

        //Draw fading path
        if (currentPointIndex > 0)
        {
            int fadeFrames = Math.Min(currentPointIndex, options.FadePointCount) - 1;
            int f = options.FadePointCount - fadeFrames;
            for (int i = currentPointIndex - fadeFrames; i <= currentPointIndex; ++i)
                if (points[i] != null && points[i - 1] != null)
                {
                    float fadePercent = (float)f++ / options.FadePointCount;
                    skPaint.Color = new SKColor(
                        (byte)((1f - fadePercent) * options.PrimaryColor.Red + fadePercent * options.SecondaryColor.Red),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Green + fadePercent * options.SecondaryColor.Green),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Blue + fadePercent * options.SecondaryColor.Blue),
                        (byte)((1f - fadePercent) * options.PrimaryColor.Alpha + fadePercent * options.SecondaryColor.Alpha));
                    canvas.DrawLine(points[i] ?? new(), points[i - 1] ?? new(), skPaint);
                    //smooth corners by drawing circles at points
                    canvas.DrawCircle(points[i - 1] ?? new(), options.StrokeWidth, skPaint);
                }
        }

        //Mark current position with a circle
        if (points[currentPointIndex] != null)
        {
            skPaint.Color = options.SecondaryColor;
            canvas.DrawCircle(points[currentPointIndex] ?? new(), options.StrokeWidth * 2, skPaint);
        }

        return bitmap;
    }


    public struct PathRendererOptions
    {
        public int BitmapWidth { get; set; }
        public int BitmapHeight { get; set; }
        public SKColor PrimaryColor { get; set; }
        public SKColor SecondaryColor { get; set; }
        public float StrokeWidth { get; set; }
        public int FadePointCount { get; set; }
    }
}
