using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using SkiaSharp;

namespace FitFileOverlay.Services;

public partial class OverlayService
{
    private SKBitmap? CreateDataFieldsOverlay(IActivityRecord record)
    {
        DataFieldRendererOptions rendererOptions = CreateDataFieldRendererOptionsFromSettings(Settings!);
        int dataFieldCount = Settings!.DrawnDataFields.Count;
        int dataFieldsPerColumn = (int)Math.Ceiling((double)dataFieldCount / Settings.DataOverlayColumnCount);
        if (Settings.DataFieldsOverlayWidth <= 0)
            return null;
        SKBitmap sKBitmap = new(Settings.DataFieldsOverlayWidth, Settings.DataFieldsOverlayHeight);
        SKCanvas sKCanvas = new(sKBitmap);
        //create data field overlays and apply them in the correct place
        int row = 0, col = 0;
        foreach (DataFieldType dataField in Settings.DrawnDataFields)
        {
            SKBitmap? dataFieldBitmap = CreateDataFieldBitmap(record, rendererOptions, dataField);
            if (dataFieldBitmap != null)
                sKCanvas.DrawBitmap(dataFieldBitmap, rendererOptions.BitmapWidth * col, rendererOptions.BitmapHeight * row, SKSamplingOptions.Default);
            if (++row >= dataFieldsPerColumn)
            {
                row = 0;
                ++col;
            }
        }
        return sKBitmap;
    }

    private SKBitmap? CreateDataFieldBitmap(IActivityRecord record, DataFieldRendererOptions rendererOptionsBase, DataFieldType dataField)
    {
        string label, value, unit;
        switch (dataField)
        {
            case DataFieldType.Pace:
                label = Settings!.PaceLabel;
                value = ConvertSpeedToPaceString(record.Speed, Settings!.PaceUnit);
                unit = Settings.PaceUnit == PaceUnit.MinutesPerKilometer ? "/km" : "/mi";
                if (Settings.UppercasePaceUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.HeartRate:
                label = Settings!.HrLabel;
                value = record.HeartRate.ToString() ?? string.Empty;
                unit = Settings.UppercaseHrUnit ? "BPM" : "bpm";
                rendererOptionsBase.ValueColor = GetHeartRateZoneBrush(record.HeartRate ?? 0);
                break;
            case DataFieldType.Distance:
                label = Settings!.DistanceLabel;
                switch (Settings.DistanceUnit)
                {
                    case DistanceUnit.Miles:
                        //meters to miles
                        value = (record.Distance / 1609.34)?.ToString("0.00") ?? string.Empty;
                        unit = "mi";
                        break;
                    case DistanceUnit.Kilometers:
                    default:
                        //meters to kilometers
                        value = (record.Distance / 1000)?.ToString("0.00") ?? string.Empty;
                        unit = "km";
                        break;
                }
                if (Settings.UppercaseDistanceUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.Cadence:
                label = Settings!.CadenceLabel;
                //half cadence (rpm) to full cadence (spm)
                value = (record.Cadence * 2)?.ToString("0") ?? string.Empty;
                unit = Settings.UppercaseCadenceUnit ? "SPM" : "spm";
                break;
            case DataFieldType.Speed:
                label = Settings!.SpeedLabel;
                switch (Settings!.SpeedUnit)
                {
                    case SpeedUnit.MilesPerHour:
                        // m/s to mph
                        value = (record.Speed * 2.23694)?.ToString("0.0") ?? string.Empty;
                        unit = "mph";
                        break;
                    case SpeedUnit.FeetPerSecond:
                        // m/s to ft/s
                        value = (record.Speed * 3.28084)?.ToString("0.0") ?? string.Empty;
                        unit = "ft/s";
                        break;
                    case SpeedUnit.MetersPerSecond:
                        // m/s
                        value = record.Speed?.ToString("0.0") ?? string.Empty;
                        unit = "m/s";
                        break;
                    case SpeedUnit.KilometersPerHour:
                    default:
                        // m/s to km/h
                        value = (record.Speed * 3.6)?.ToString("0.0") ?? string.Empty;
                        unit = "km/h";
                        break;
                }
                if (Settings.UppercaseSpeedUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.Power:
                label = Settings!.PowerLabel;
                value = record.Power?.ToString() ?? string.Empty;
                unit = "W";
                break;
            case DataFieldType.StrideLength:
                label = Settings!.StrideLengthLabel;
                switch (Settings!.StrideLengthUnit)
                {
                    case StrideLengthUnit.Feet:
                        // milimeters to feet
                        value = (record.StrideLength / 304.8)?.ToString("0.00") ?? string.Empty;
                        unit = "ft";
                        break;
                    case StrideLengthUnit.Centimeters:
                        // milimeters to centimeters
                        value = (record.StrideLength / 10)?.ToString("0") ?? string.Empty;
                        unit = "cm";
                        break;
                    case StrideLengthUnit.Inches:
                        // milimeters to inches
                        value = (record.StrideLength / 25.4)?.ToString("0.0") ?? string.Empty;
                        unit = "in";
                        break;
                    case StrideLengthUnit.Meters:
                    default:
                        // milimeters to meters
                        value = (record.StrideLength / 1000)?.ToString("0.00") ?? string.Empty;
                        unit = "m";
                        break;
                }
                if (Settings.UppercaseStrideLengthUnit)
                    unit = unit.ToUpper();
                break;
            case DataFieldType.Timestamp:
                label = string.Empty;
                value = record.TimeStamp.ToLocalTime().ToString("dd-MMM-yy H:mm:ss");
                unit = string.Empty;
                rendererOptionsBase.ValueFont =
                    new SKFont(SKTypeface.FromFamilyName(
                            familyName: Settings!.ValueFontFamily,
                            weight: Settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                            width: SKFontStyleWidth.Normal,
                            slant: Settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                        Settings.TimestampFontSize);
                break;
            default:
                return null;
        }
        return DataFieldRenderer.Render(rendererOptionsBase, label, value, unit);
    }

    private SKColor GetHeartRateZoneBrush(int heartRate)
    {
        int lthr = File!.LactateThresholdHeartRate ?? Settings!.LTHR;
        if (Settings!.UseCustomLTHR)
            lthr = Settings.LTHR;
        float percentLthr = (float)heartRate / lthr;
        int zoneIndex = 0;
        for (int i = 0; i < Settings.ZoneMaxPercent.Length; ++i)
            if (percentLthr <= Settings.ZoneMaxPercent[i])
            {
                zoneIndex = i;
                break;
            }
        return zoneIndex switch
        {
            0 => Settings.Zone1Brush,
            1 => Settings.Zone2Brush,
            2 => Settings.Zone3Brush,
            3 => Settings.Zone4Brush,
            4 => Settings.Zone5Brush,
            _ => SKColors.Transparent,
        };
    }

    private static string ConvertSpeedToPaceString(float? speed, PaceUnit unit)
    {
        var secPerUnitDistance = unit switch
        {
            PaceUnit.MinutesPerMile => (int)(1609.34 / (speed ?? 0f)),
            _ => (int)(1000 / (speed ?? 0f)),
        };
        string paceString;
        if (secPerUnitDistance < 1 || secPerUnitDistance > 3600)
            paceString = "--'--\"";
        else
            paceString = $"{secPerUnitDistance / 60}'{secPerUnitDistance % 60:D2}\"";
        return paceString;
    }

    private static DataFieldRendererOptions CreateDataFieldRendererOptionsFromSettings(OverlaySettings settings)
    {
        int dataOverlayWidth = settings.DataFieldsOverlayWidth / settings.DataOverlayColumnCount;
        int dataOverlayHeight = (int)(settings.ValueFontSize + settings.LabelFontSize + settings.LineSpacing + settings.DataOverlayVerticalSpacing);
        DataFieldRendererOptions dataFieldRendererOptions = new()
        {
            BitmapHeight = dataOverlayHeight,
            BitmapWidth = dataOverlayWidth,
            LabelColor = settings.PrimaryColor,
            LabelFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.LabelFontFamily,
                    weight: settings.IsLabelFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsLabelFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.LabelFontSize),
            ValueColor = settings.PrimaryColor,
            ValueFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.ValueFontFamily,
                    weight: settings.IsValueFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsValueFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.ValueFontSize),
            UnitColor = settings.SecondaryColor,
            UnitFont = new SKFont(
                SKTypeface.FromFamilyName(
                    familyName: settings.UnitFontFamily,
                    weight: settings.IsUnitFontBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                    width: SKFontStyleWidth.Normal,
                    slant: settings.IsUnitFontItalic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright),
                settings.UnitFontSize),
            LineSpacing = settings.LineSpacing
        };
        return dataFieldRendererOptions;
    }
}
