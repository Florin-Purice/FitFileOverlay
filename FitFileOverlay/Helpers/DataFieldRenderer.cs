using SkiaSharp;

namespace FitFileOverlay.Helpers;

public class DataFieldRenderer
{
    public static SKBitmap Render(DataFieldRendererOptions options, string label, string value, string unit)
    {
        SKBitmap bitmap = new(options.BitmapWidth, options.BitmapHeight);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint skPaint = new();
        skPaint.IsAntialias = true;

        //draw label
        skPaint.Color = options.LabelColor;
        options.LabelFont.MeasureText(label, out SKRect labelSize, skPaint);
        labelSize.Offset(0, options.LabelFont.Size + options.LineSpacing);
        canvas.DrawText(label, labelSize.Left, labelSize.Bottom, SKTextAlign.Left, options.LabelFont, skPaint);
        //draw value
        skPaint.Color = options.ValueColor;
        options.ValueFont.MeasureText(value, out SKRect valueSize, skPaint);
        valueSize.Offset(0, labelSize.Bottom + options.ValueFont.Size - valueSize.Bottom/*fix font irregularities*/ + options.LineSpacing);
        canvas.DrawText(value, valueSize.Left, valueSize.Bottom, SKTextAlign.Left, options.ValueFont, skPaint);
        //draw unit
        skPaint.Color = options.UnitColor;
        options.UnitFont.MeasureText(unit, out SKRect unitSize, skPaint);
        unitSize.Offset(valueSize.Right, valueSize.Bottom);
        canvas.DrawText(unit, unitSize.Left, unitSize.Bottom, SKTextAlign.Left, options.UnitFont, skPaint);

        return bitmap;
    }
}

public struct DataFieldRendererOptions
{
    public int BitmapWidth { get; set; }
    public int BitmapHeight { get; set; }
    public SKColor LabelColor { get; set; }
    public SKColor ValueColor { get; set; }
    public SKColor UnitColor { get; set; }
    public SKFont LabelFont { get; set; }
    public SKFont ValueFont { get; set; }
    public SKFont UnitFont { get; set; }
    public float LineSpacing { get; set; }
}
