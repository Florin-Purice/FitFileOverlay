using FitFileOverlay.Navigation;
using FitFileOverlay.Overlay;
using FitFileOverlay.Pages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace FitFileOverlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly string _overlaySettingsFilename = "overlay_settings.json";

    [STAThread]
    private static void Main(string[] args)
    {
        MainAsync(args).GetAwaiter().GetResult();
    }

    private static async Task MainAsync(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        await host.StartAsync().ConfigureAwait(true);

        App app = new();
        app.InitializeComponent();
        app.Exit += (s, e) =>
            host.Services.GetRequiredService<OverlaySettings>().ToFile(_overlaySettingsFilename);
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        host.Services.GetRequiredService<INavigationManager>().NavigateTo(NavigationTarget.Home);
        app.Run();

        await host.StopAsync().ConfigureAwait(true);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder)
            => configurationBuilder.AddUserSecrets(typeof(App).Assembly))
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<OverlaySettings>(s => OverlaySettings.FromFile(_overlaySettingsFilename) ?? new OverlaySettings());
            services.AddSingleton<IOverlayService, OverlayService>();
            services.AddSingleton<INavigationManager>(s => CreateNavigationManager(s));
            services.AddSingleton<HomePageViewModel>();
            services.AddSingleton<SettingsPageViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainWindow>();
        });

    private static SimpleNavigationManager CreateNavigationManager(IServiceProvider s)
    {
        SimpleNavigationManager manager = new();
        manager.RegisterViewModelFactory(NavigationTarget.Home, () => s.GetRequiredService<HomePageViewModel>());
        manager.RegisterViewModelFactory(NavigationTarget.Settings, () => s.GetRequiredService<SettingsPageViewModel>());
        return manager;
    }
}
