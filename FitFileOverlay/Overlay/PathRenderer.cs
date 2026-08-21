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
            if (points[i] != null &&  points[i + 1] != null)
            {
                canvas.DrawLine(points[i] ?? new(), points[i+1] ?? new(), skPaint);
                //smooth corners by drawing circles at points
                canvas.DrawCircle(points[i] ?? new(), options.StrokeWidth / 2f, skPaint);
                canvas.DrawCircle(points[i+1] ?? new(), options.StrokeWidth / 2f, skPaint);
            }

        return bitmap;
    }

    public static SKBitmap RenderUntilPoint(PathRendererOptions options, List<SKPoint?> points, int currentPointIndex)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth;
        skPaint.Color = options.PrimaryColor;

        for (int i = 0; i < currentPointIndex - 1 && i < points.Count - 1; ++i)
            if (points[i] != null && points[i + 1] != null)
            {
                canvas.DrawLine(points[i] ?? new(), points[i + 1] ?? new(), skPaint);
                //smooth corners by drawing circles at points
                canvas.DrawCircle(points[i] ?? new(), options.StrokeWidth / 2f, skPaint);
                canvas.DrawCircle(points[i + 1] ?? new(), options.StrokeWidth / 2f, skPaint);
            }
        //Draw fading path
        for(int i = currentPointIndex, fade = 0; i > 0 && fade < options.FadePointCount; --i, ++fade)
            if (points[i] != null && points[i - 1] != null)
            {
                float fadePercent = (float)fade / options.FadePointCount;
                skPaint.Color = new SKColor(
                    (byte)((1f - fadePercent) * options.SecondaryColor.Red + fadePercent * options.PrimaryColor.Red),
                    (byte)((1f - fadePercent) * options.SecondaryColor.Green + fadePercent * options.PrimaryColor.Green),
                    (byte)((1f - fadePercent) * options.SecondaryColor.Blue + fadePercent * options.PrimaryColor.Blue),
                    (byte)((1f - fadePercent) * options.SecondaryColor.Alpha + fadePercent * options.PrimaryColor.Alpha));
                canvas.DrawLine(points[i] ?? new(), points[i - 1] ?? new(), skPaint);
                //smooth corners by drawing circles at points
                canvas.DrawCircle(points[i] ?? new(), options.StrokeWidth / 2f, skPaint);
                canvas.DrawCircle(points[i - 1] ?? new(), options.StrokeWidth / 2f, skPaint);
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
