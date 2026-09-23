using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Services;

public partial class OverlayService
{
    private void PrepareAltitudeOverlayData(ref InstanceData data)
    {
        float? startDistance = data.Records.First().Distance;
        float? totalDistance = data.Records.Last().Distance - startDistance;
        for (int i = 0; i < data.Records.Count; i++)
        {
            data.AltitudeValues.Add(data.Records[i].Altitude);
            float? xPos = Settings!.AltitudeXReference switch
            {
                AltitudeXReference.Distance => (data.Records[i].Distance - startDistance) / totalDistance,
                _ => (float)i / (data.Records.Count - 1)
            };
            data.AltitudeXPositions.Add(xPos);
        }
        data.AltitudeBaseBitmap = GraphRenderer.RenderStaticPart(data.GraphRendererOptions, data.AltitudeValues, data.AltitudeXPositions);
    }

    private static Func<float?, float?> GetAltitudeValueConverter(AltitudeUnit unit)
    {
        return unit switch
        {
            AltitudeUnit.Feet => (x) => x * 3.28084f,
            _ => (x) => x //default meters
        };
    }

    private static GraphRendererOptions CreateGraphRendererOptionsFromSettings(OverlaySettings settings)
    {
        GraphRendererOptions graphRendererOptions = new()
        {
            BitmapWidth = settings.AltitudeOverlayWidth,
            BitmapHeight = settings.AltitudeOverlayHeight,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            TertiaryColor = settings.TertiaryColor,
            BackgroundAlpha = settings.AltitudeBackgroundAlpha,
            StrokeWidth = settings.AltitudeLineWidth,
            BottomPaddingPercent = settings.AltitudeBottomPaddingPercent,
            MinimumRange = settings.AltitudeMinimumRange,
            ValueFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.ValueFontFamily,
                    weight: settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.AltitudeValueFontSize),
            UnitFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.UnitFontFamily,
                    weight: settings.IsUnitFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsUnitFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.AltitudeUnitFontSize),
            UnitText = settings.AltitudeUnit switch
            {
                AltitudeUnit.Feet => "ft",
                _ => "m"
            },
            IsTextEnabled = settings.IsAltitudeTextEnabled,
            FadePointCount = (int)(settings.FadeDurationSeconds * settings.FPS)
        };
        if (settings.UppercaseAltitudeUnit)
            graphRendererOptions.UnitText = graphRendererOptions.UnitText.ToUpper();
        return graphRendererOptions;
    }
}
