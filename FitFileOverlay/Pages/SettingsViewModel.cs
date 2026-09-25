using FitFileOverlay.Enums;
using FitFileOverlay.Models;
using System.Reflection;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace FitFileOverlay.Pages;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IContentDialogService _contentDialogService;
    private readonly IThemeService _themeService;

    public SettingsViewModel(IContentDialogService contentDialogService, IThemeService themeService)
    {
        _contentDialogService = contentDialogService;
        _themeService = themeService;
        AppThemeValues = Enum.GetValues<AppTheme>();
        AppVersion = $"FitFileOverlay - {GetAssemblyVersion()}";
        AppSettings = App.AppSettings;
        AppSettings.PropertyChanged += OnAppSettingsPropertyChanged;
    }

    [ObservableProperty]
    public partial AppSettings AppSettings { get; private set; }
    [ObservableProperty]
    public partial AppTheme[] AppThemeValues { get; private set; }
    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }

    private void OnAppSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Theme))
            App.ChangeTheme(AppSettings.Theme);
    }
}
