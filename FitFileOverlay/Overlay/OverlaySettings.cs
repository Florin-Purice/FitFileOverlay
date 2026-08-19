using System.Drawing;

using SkiaSharp;

namespace FitFileOverlay.Overlay;

public class OverlaySettings
{
    public int LTHR { get; set; } = 145;
    public uint FPS { get; set; } = 1;
    public string PaceLabel { get; set; } = "Pace";
    public string PaceUnit { get; set; } = "/KM";
    public string HrLabel { get; set; } = "Heart Rate";
    public string HrUnit { get; set; } = "BPM";
    public string DistanceLabel { get; set; } = "Distance";
    public string DistanceUnit { get; set; } = "KM";
    public int LineSpacingPixels { get; set; } = 20;
    public float FontSizeSmall { get; set; } = 48f;
    public float FontSizeBig { get; set; } = 96f;
    public float GpsLineWidth { get; set; } = 6f;
    public int GpsFadeDurationSeconds { get; set; } = 180;
    public string LabelFontFamily { get; set; } = "Arial";
    public string ValueFontFamily { get; set; } = "Impact";
    public SKColor PrimaryColor { get; set; } = SKColors.White;
    public SKColor SecondaryColor { get; set; } = SKColors.Orange;
    public SKColor GpsOutlineColor { get; set; } = new SKColor(127, 127, 127, 200);
    public Size BitmapSize { get; set; } = new Size(1600, 800);
    public Size GPSBitmapSize { get; set; } = new Size(800, 800);
    public SKColor[] ZoneBrushes { get; set; } =
    [
        new SKColor(166, 166, 166, 255), // Zone 1 - Gray
            new SKColor(59, 151, 243, 255), // Zone 2 - Blue
            new SKColor(130, 201, 30, 255), // Zone 3 - Green
            new SKColor(249, 137, 37, 255), // Zone 4 - Orange
            new SKColor(211, 32, 32, 255) // Zone 5 - Red
    ];
    public float[] ZoneMaxPercent { get; set; } = [0.8f, 0.89f, 0.95f, 1f, float.MaxValue];
}
