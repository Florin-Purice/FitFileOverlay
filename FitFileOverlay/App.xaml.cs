using CommunityToolkit.Mvvm.Messaging;
using FitFileOverlay.Enums;
using FitFileOverlay.Models;
using FitFileOverlay.Pages;
using FitFileOverlay.Services;
using FitFileOverlay.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Velopack;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

namespace FitFileOverlay;

public partial class App
{
    private static OverlaySettings? _overlaySettings;
    private static AppSettings? _appSettings;
    private static readonly string _overlaySettingsFilename = "overlay_settings.json";
    private static readonly string _appSettingsFilename = "app_settings.json";
#if DEBUG
    private static readonly string _settingsLocation = @".\";
#else
    private static readonly string _settingsLocation = @"..\";
#endif

    public static AppSettings AppSettings => _appSettings ?? new();
    public static OverlaySettings OverlaySettings => _overlaySettings ?? new();
    public static IServiceProvider Services => _host.Services;

    public static void ChangeTheme(AppTheme theme)
    {
        if (theme == AppTheme.System)
            SystemThemeWatcher.Watch(App.Current.MainWindow);
        else
            SystemThemeWatcher.UnWatch(App.Current.MainWindow);

        switch (theme)
        {
            case AppTheme.Light:
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                break;
            case AppTheme.Dark:
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                break;
            case AppTheme.HighContrast:
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                break;
            case AppTheme.System:
            default:
                ApplicationThemeManager.ApplySystemTheme();
                break;
        }
    }

    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)!); })
        .ConfigureServices((context, services) =>
        {
            services.AddNavigationViewPageProvider();

            services.AddSingleton<IMessenger, WeakReferenceMessenger>();

            services.AddSingleton<IContentDialogService, ContentDialogService>();

            services.AddHostedService<ApplicationHostService>();

            // Theme manipulation
            services.AddSingleton<IThemeService, ThemeService>();

            // TaskBar manipulation
            services.AddSingleton<ITaskBarService, TaskBarService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();

            // Main window with navigation
            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<HomePage>();
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<CropPage>();
            services.AddSingleton<CropPageViewModel>();
            services.AddSingleton<EditPage>();
            services.AddSingleton<EditPageViewModel>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<SettingsViewModel>();

            services.AddSingleton<IOverlayService>(s => new OverlayService { Settings = OverlaySettings });
        }).Build();

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        VelopackApp.Build().Run();
        LoadSettings();

        await _host.StartAsync();

        ChangeTheme(AppSettings.Theme);
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        SaveSettings();

        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
    }

    private static void SaveSettings()
    {
        AppSettings.ToFile(Path.Combine(_settingsLocation, _appSettingsFilename));
        OverlaySettings.ToFile(Path.Combine(AppSettings.SaveLocation, _overlaySettingsFilename));
    }

    private static void LoadSettings()
    {
        _appSettings = AppSettings.FromFile(Path.Combine(_settingsLocation, _appSettingsFilename));
        _overlaySettings = OverlaySettings.FromFile(Path.Combine(AppSettings.SaveLocation, _overlaySettingsFilename));
    }
}
