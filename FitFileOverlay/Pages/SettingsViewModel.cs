using FitFileOverlay.Services;
using FitFileOverlay.Windows;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Reflection;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace FitFileOverlay.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private bool _isInitialized = false;
    private readonly PreviewWindowViewModel _previewWindowViewModel;
    private PreviewWindow? _previewWindow;

    public SettingsViewModel(IOverlayService overlayService)
    {
        OverlayService = overlayService;
        _previewWindowViewModel = new PreviewWindowViewModel(OverlayService);
        FontFamilies = new ObservableCollection<string>(SKFontManager.Default.FontFamilies);
    }

    [ObservableProperty]
    public partial IOverlayService OverlayService { get; private set; }

    [ObservableProperty]
    public partial string AppVersion { get; set; } = String.Empty;

    [ObservableProperty]
    public partial ApplicationTheme CurrentTheme { get; set; } = ApplicationTheme.Unknown;

    [ObservableProperty]
    public partial ObservableCollection<string> FontFamilies { get; set; }

    public Task OnNavigatedToAsync()
    {
        if (!_isInitialized)
            InitializeViewModel();
        //open a preview window
        _previewWindow = new PreviewWindow(_previewWindowViewModel);
        _previewWindow.Show();

        return Task.CompletedTask;
    }

    public Task OnNavigatedFromAsync()
    {
        _previewWindow?.Close();
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
