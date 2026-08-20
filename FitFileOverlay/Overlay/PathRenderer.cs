using SkiaSharp;

namespace FitFileOverlay.Overlay;

public class PathRenderer
{
    public static SKBitmap RenderFull(PathRendererOptions options, List<SKPoint> points)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(options.Background);
        canvas.SaveLayer();
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth;
        skPaint.Color = options.PrimaryColor;
        skPaint.BlendMode = SKBlendMode.Src;

        for (int i = 0; i < points.Count - 1; ++i)
        {
            canvas.DrawLine(points[i], points[i+1], skPaint);
            //smooth corners by drawing circles at points
            canvas.DrawCircle(points[i], options.StrokeWidth / 2f, skPaint);
            canvas.DrawCircle(points[i+1], options.StrokeWidth / 2f, skPaint);
        }

        canvas.Restore();
        return bitmap;
    }

    public static SKBitmap RenderUntilPoint(PathRendererOptions options, List<SKPoint> points, int currentPointIndex)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(options.Background);
        canvas.SaveLayer();
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;
        skPaint.BlendMode = SKBlendMode.Src;
        skPaint.StrokeWidth = options.StrokeWidth;
        skPaint.Color = options.PrimaryColor;

        for (int i = 0; i < currentPointIndex - 1 && i < points.Count - 1; ++i)
        {
            canvas.DrawLine(points[i], points[i + 1], skPaint);
            //smooth corners by drawing circles at points
            canvas.DrawCircle(points[i], options.StrokeWidth / 2f, skPaint);
            canvas.DrawCircle(points[i + 1], options.StrokeWidth / 2f, skPaint);
        }
        //Draw fading path
        for(int i = currentPointIndex, fade = 0; i > 0 && fade < options.FadePointCount; --i, ++fade)
        {
            float fadePercent = (float)fade / options.FadePointCount;
            skPaint.Color = new SKColor(
                (byte)((1f - fadePercent) * options.SecondaryColor.Red + fadePercent * options.PrimaryColor.Red),
                (byte)((1f - fadePercent) * options.SecondaryColor.Green + fadePercent * options.PrimaryColor.Green),
                (byte)((1f - fadePercent) * options.SecondaryColor.Blue + fadePercent * options.PrimaryColor.Blue),
                (byte)((1f - fadePercent) * options.SecondaryColor.Alpha + fadePercent * options.PrimaryColor.Alpha));
            canvas.DrawLine(points[i], points[i - 1], skPaint);
            //smooth corners by drawing circles at points
            canvas.DrawCircle(points[i], options.StrokeWidth / 2f, skPaint);
            canvas.DrawCircle(points[i - 1], options.StrokeWidth / 2f, skPaint);
        }
        //Mark current position with a circle
        skPaint.Color = options.SecondaryColor;
        canvas.DrawCircle(points[currentPointIndex], options.StrokeWidth * 2, skPaint);

        canvas.Restore();
        return bitmap;
    }


    public struct PathRendererOptions
    {
        public int BitmapWidth { get; set; }
        public int BitmapHeight { get; set; }
        public SKColor Background { get; set; }
        public SKColor PrimaryColor { get; set; }
        public SKColor SecondaryColor { get; set; }
        public float StrokeWidth { get; set; }
        public int FadePointCount { get; set; }
    }
}
