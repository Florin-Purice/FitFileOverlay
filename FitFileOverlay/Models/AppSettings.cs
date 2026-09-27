using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using System.IO;

namespace FitFileOverlay.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty]
    public partial AppTheme Theme { get; set; } = AppTheme.System;
    [ObservableProperty]
    public partial bool UpdateAtStartup { get; set; } = true;
#if DEBUG
    [ObservableProperty]
    public partial string SaveLocation { get; set; } = @".\";
#else
    [ObservableProperty]
    public partial string SaveLocation { get; set; } = @"..\";
#endif

    public void ToFile(string fileName)
    {
        string jsonString = CustomJsonSerializer.Serialize(this);
        string? directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory!);
        File.WriteAllText(fileName, jsonString);
    }

    public static AppSettings? FromFile(string fileName)
    {
        try
        {
            string jsonString = File.ReadAllText(fileName);
            return CustomJsonSerializer.Deserialize<AppSettings>(jsonString);
        }
        catch
        {
            return null;
        }
    }
}
