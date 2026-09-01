using FitFileOverlay.Controls;
using FitFileOverlay.Enums;
using FitFileOverlay.Models;
using FitFileOverlay.Services;
using FitFileOverlay.Windows;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace FitFileOverlay.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private bool _isInitialized = false;
    private readonly string _templatesDirectory = "./Templates/";
    private readonly PreviewWindowViewModel _previewWindowViewModel;
    private readonly IContentDialogService _contentDialogService;
    private PreviewWindow? _previewWindow;

    public SettingsViewModel(IOverlayService overlayService, IContentDialogService contentDialogService)
    {
        OverlayService = overlayService;
        _contentDialogService = contentDialogService;
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
    private void ToggleDataFieldVisibility(DataFieldType item)
    {
        if(OverlayService.Settings is not null)
            if (!OverlayService.Settings.DrawnDataFields.Remove(item))
                OverlayService.Settings.DrawnDataFields.Add(item);
    }

    [RelayCommand]
    private async Task SaveTemplate()
    {
        SaveTemplateDialogContent content = new();
        ContentDialog dialog = new()
        {
            Title = "Save settings as template",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Save"
        };
        ContentDialogResult result = await _contentDialogService.ShowAsync(dialog, default);
        if (result == ContentDialogResult.Primary)
        {
            string fileName = content.FileName;
            if (fileName == "_default_" || fileName == "_default_.json")
                fileName = "default";
            if (!string.IsNullOrWhiteSpace(fileName))
                OverlayService.Settings?.ToFile(_templatesDirectory + fileName + (fileName.EndsWith(".json") ? "" : ".json"));
        }
    }

    [RelayCommand]
    private async Task LoadTemplate()
    {
        Dictionary<string, OverlaySettings> templates = [];
        templates.Add("_default_", new OverlaySettings());//default template
        if (Directory.Exists(_templatesDirectory))
            foreach (string settingsFilename in Directory.GetFiles(_templatesDirectory, "*.json"))
                try
                {
                    string templateName = Path.GetFileNameWithoutExtension(settingsFilename);
                    OverlaySettings? t = OverlaySettings.FromFile(settingsFilename);
                    if (t != null)
                        templates.Add(templateName, t);
                }
                catch { }
        LoadTemplateDialogContent content = new() { Templates = templates };
        ContentDialog dialog = new()
        {
            Title = "Load template",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Load"
        };
        ContentDialogResult result = await _contentDialogService.ShowAsync(dialog, default);
        if (result == ContentDialogResult.Primary)
        {
            KeyValuePair<string, OverlaySettings> templateKV = content.SelectedValue;
            OverlayService.Settings = templateKV.Value;
        }
    }

    [RelayCommand]
    private void MoveFieldUp(DataFieldType item)
    {
        if(OverlayService.Settings is null)
            return;
        int index = OverlayService.Settings.DrawnDataFields.IndexOf(item);
        if (index > 0)
            OverlayService.Settings.DrawnDataFields.Move(index, index - 1);
        else
            OverlayService.Settings.DrawnDataFields.Move(index, OverlayService.Settings.DrawnDataFields.Count - 1);
    }

    [RelayCommand]
    private void MoveFieldDown(DataFieldType item)
    {
        if (OverlayService.Settings is null)
            return;
        int index = OverlayService.Settings.DrawnDataFields.IndexOf(item);
        if (index < OverlayService.Settings.DrawnDataFields.Count - 1)
            OverlayService.Settings.DrawnDataFields.Move(index, index + 1);
        else
            OverlayService.Settings.DrawnDataFields.Move(index, 0);
    }

    [RelayCommand]
    private void OpenPreviewWindow()
    {
        if (_previewWindow is null)
        {
            _previewWindow = new PreviewWindow(_previewWindowViewModel);
            _previewWindow.Closed += (_, _) => _previewWindow = null;
            _previewWindow.Show();
        }
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
