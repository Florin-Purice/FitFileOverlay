using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.IO;

namespace FitFileOverlay.Models;

public partial class OverlaySettings : ObservableObject
{
    public OverlaySettings()
    {
        DrawnDataFields.CollectionChanged += OnDrawnDataFieldsCollectionChanged;   
    }

    #region GENERAL
    [ObservableProperty]
    public partial int LTHR { get; set; } = 145;
    [ObservableProperty]
    public partial bool UseCustomLTHR { get; set; } = false;
    [ObservableProperty]
    public partial uint FPS { get; set; } = 5;
    [ObservableProperty]
    public partial SKColor Background { get; set; } = SKColors.Transparent;
    [ObservableProperty]
    public partial SKColor PrimaryColor { get; set; } = SKColors.White;
    [ObservableProperty]
    public partial SKColor SecondaryColor { get; set; } = SKColors.Orange;
    [ObservableProperty]
    public partial int OverlayHeight { get; set; } = 800;
    [ObservableProperty]
    public partial bool IsDataFieldsOverlayEnabled { get; set; } = true;
    [ObservableProperty]
    public partial int DataFieldsOverlayWidth { get; set; } = 800;
    [ObservableProperty]
    public partial bool IsGpsOverlayEnabled { get; set; } = true;
    [ObservableProperty]
    public partial int GpsOverlayWidth { get; set; } = 800;
    #endregion
    #region MAP
    [ObservableProperty]
    public partial float GpsLineWidth { get; set; } = 6f;
    [ObservableProperty]
    public partial int GpsFadeDurationSeconds { get; set; } = 180;
    [ObservableProperty]
    public partial SKColor GpsOutlineColor { get; set; } = new SKColor(127, 127, 127, 200);
    #endregion
    #region DATA_FIELD
    [ObservableProperty]
    public partial int LineSpacing { get; set; } = 15;
    [ObservableProperty]
    public partial int DataOverlayColumnCount { get; set; } = 2;
    [ObservableProperty]
    public partial int DataOverlayVerticalSpacing { get; set; } = 40;
    [ObservableProperty]
    public partial string LabelFontFamily { get; set; } = "Arial";
    [ObservableProperty]
    public partial float LabelFontSize { get; set; } = 48f;
    [ObservableProperty]
    public partial bool IsLabelFontBold { get; set; } = false;
    [ObservableProperty]
    public partial bool IsLabelFontItalic { get; set; } = false;
    [ObservableProperty]
    public partial string ValueFontFamily { get; set; } = "Impact";
    [ObservableProperty]
    public partial float ValueFontSize { get; set; } = 96f;
    [ObservableProperty]
    public partial bool IsValueFontBold { get; set; } = false;
    [ObservableProperty]
    public partial bool IsValueFontItalic { get; set; } = true;

    [ObservableProperty]
    public partial string UnitFontFamily { get; set; } = "Impact";
    [ObservableProperty]
    public partial float UnitFontSize { get; set; } = 48f;
    [ObservableProperty]
    public partial bool IsUnitFontBold { get; set; } = false;
    [ObservableProperty]
    public partial bool IsUnitFontItalic { get; set; } = true;
    //Pace stuff
    [ObservableProperty]
    public partial string PaceLabel { get; set; } = "Pace";
    [ObservableProperty]
    public partial string PaceUnit { get; set; } = "/KM";
    //Distance stuff
    [ObservableProperty]
    public partial string DistanceLabel { get; set; } = "Distance";
    [ObservableProperty]
    public partial string DistanceUnit { get; set; } = "KM";
    //HR stuff
    [ObservableProperty]
    public partial string HrLabel { get; set; } = "Heart Rate";
    [ObservableProperty]
    public partial string HrUnit { get; set; } = "BPM";
    [ObservableProperty]
    public partial SKColor Zone1Brush { get; set; } = new SKColor(166, 166, 166, 255); // Zone 1 - Gray
    [ObservableProperty]
    public partial SKColor Zone2Brush { get; set; } = new SKColor(59, 151, 243, 255); // Zone 2 - Blue
    [ObservableProperty]
    public partial SKColor Zone3Brush { get; set; } = new SKColor(130, 201, 30, 255); // Zone 3 - Green
    [ObservableProperty]
    public partial SKColor Zone4Brush { get; set; } = new SKColor(249, 137, 37, 255); // Zone 4 - Orange
    [ObservableProperty]
    public partial SKColor Zone5Brush { get; set; } = new SKColor(211, 32, 32, 255); // Zone 5 - Red
    //Cadence stuff
    [ObservableProperty]
    public partial string CadenceLabel { get; set; } = "Cadence";
    [ObservableProperty]
    public partial string CadenceUnit { get; set; } = "SPM";
    //Speed stuff
    [ObservableProperty]
    public partial string SpeedLabel { get; set; } = "Speed";
    [ObservableProperty]
    public partial string SpeedUnit { get; set; } = "KM/H";
    //Power stuff
    [ObservableProperty]
    public partial string PowerLabel { get; set; } = "Power";
    [ObservableProperty]
    public partial string PowerUnit { get; set; } = "W";
    //Stride length stuff
    [ObservableProperty]
    public partial string StrideLengthLabel { get; set; } = "Stride Length";
    [ObservableProperty]
    public partial string StrideLengthUnit { get; set; } = "M";
    //Timestamp
    [ObservableProperty]
    public partial float TimestampFontSize { get; set; } = 32f;

    [ObservableProperty]
    public partial ObservableCollection<DataFieldType> DrawnDataFields { get; set; } = [
        DataFieldType.Pace,
        DataFieldType.HeartRate,
        DataFieldType.Distance,
        DataFieldType.Cadence,
        DataFieldType.Speed,
        DataFieldType.Power,
        DataFieldType.StrideLength,
        DataFieldType.Timestamp];

    #endregion

    public float[] ZoneMaxPercent { get; set; } = [0.8f, 0.89f, 0.95f, 1f, float.MaxValue];

    public void ToFile(string fileName)
    {
        string jsonString = CustomJsonSerializer.Serialize(this);
        string? directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory!);
        File.WriteAllText(fileName, jsonString);
    }

    public static OverlaySettings? FromFile(string fileName)
    {
        try
        {
            string jsonString = File.ReadAllText(fileName);
            return CustomJsonSerializer.Deserialize<OverlaySettings>(jsonString);
        }
        catch
        {
            return null;
        }
    }

    private void OnDrawnDataFieldsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(DrawnDataFields));
    }

    partial void OnDrawnDataFieldsChanged(ObservableCollection<DataFieldType> oldValue, ObservableCollection<DataFieldType> newValue)
    {
        oldValue.CollectionChanged -= OnDrawnDataFieldsCollectionChanged;
        newValue.CollectionChanged += OnDrawnDataFieldsCollectionChanged;
    }
}
