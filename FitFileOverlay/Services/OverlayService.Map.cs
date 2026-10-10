using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Services;

public partial class OverlayService
{
    private void PrepareGpsOverlayData(ref InstanceData data)
    {
        //create list of unitary screenspace gps points
        List<(double x, double y)?> normalizedGpsPoints = ProcessGpsPoints(data.Records, out double gpsAspectRatio);
        float gpsDrawAreaPadding = Settings!.GpsLineWidth * 3f;//add some padding so the points on the border dont get cut off
        double gpsDrawAreaAspectRatio = (double)(data.MapRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2) / (data.MapRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2);
        double scale;
        if (gpsDrawAreaAspectRatio > gpsAspectRatio)
            scale = data.MapRendererOptions.BitmapHeight - gpsDrawAreaPadding * 2;// points cover the full height
        else
            scale = data.MapRendererOptions.BitmapWidth - gpsDrawAreaPadding * 2;// points cover the full width
        //transform points into actual draw points
        foreach ((double x, double y)? point in normalizedGpsPoints)
            if (point is null)
                data.DrawPoints.Add(null);
            else
            {
                float x = (float)((point?.x ?? 0) * scale + gpsDrawAreaPadding);
                float y = (float)((point?.y ?? 0) * scale + gpsDrawAreaPadding);
                data.DrawPoints.Add(new SKPoint(x, y));
            }
        //create renderer instance and base bitmap
        data.AltitudeRenderer = new PathRenderer(data.MapRendererOptions, data.DrawPoints);
        data.MapBaseBitmap = new SKBitmap(data.MapRendererOptions.BitmapWidth, data.MapRendererOptions.BitmapHeight);
        using SKCanvas canvas = new(data.MapBaseBitmap);
        data.AltitudeRenderer.RenderStaticPart(canvas);
    }

    private static List<(double x, double y)?> ProcessGpsPoints(ICollection<IActivityRecord> records, out double gpsAspectRatio)
    {
        List<GpsPoint?> points = [];
        foreach (IActivityRecord record in records)
            points.Add(record.GPSPoint);
        return GpsPoint.PointsListToUnitaryScreenSpace(points, out gpsAspectRatio);
    }

    private static PathRendererOptions CreatePathRendererOptionsFromSettings(OverlaySettings settings)
    {
        PathRendererOptions pathRendererOptions = new()
        {
            BitmapWidth = settings.GpsOverlayWidth,
            BitmapHeight = settings.GpsOverlayHeight,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            TertiaryColor = settings.TertiaryColor,
            StrokeWidth = settings.GpsLineWidth,
            FadePointCount = (int)(settings.FadeDurationSeconds * settings.FPS)
        };
        return pathRendererOptions;
    }
}
