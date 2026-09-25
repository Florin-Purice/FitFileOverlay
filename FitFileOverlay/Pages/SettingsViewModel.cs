using System.Reflection;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace FitFileOverlay.Pages;

public partial class SettingsViewModel(IContentDialogService contentDialogService) : ObservableObject, INavigationAware
{
    private bool _isInitialized = false;

    [ObservableProperty]
    public partial string AppVersion { get; set; } = String.Empty;
    [ObservableProperty]
    public partial ApplicationTheme CurrentTheme { get; set; } = ApplicationTheme.Unknown;

    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
            InitializeViewModel();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    private void InitializeViewModel()
    {
        CurrentTheme = ApplicationThemeManager.GetAppTheme();
        AppVersion = $"FitFileOverlay - {GetAssemblyVersion()}";
        _isInitialized = true;
    }

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? String.Empty;
    }

    [RelayCommand]
    private void OnChangeTheme(string parameter)
    {
        switch (parameter)
        {
            case "theme_light":
                if (CurrentTheme == ApplicationTheme.Light)
                    break;
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                CurrentTheme = ApplicationTheme.Light;
                break;
            case "theme_high_contrast":
                if (CurrentTheme == ApplicationTheme.HighContrast)
                    break;
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                CurrentTheme = ApplicationTheme.HighContrast;
                break;
            default:
                if (CurrentTheme == ApplicationTheme.Dark)
                    break;
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                CurrentTheme = ApplicationTheme.Dark;
                break;
        }
    }
}
