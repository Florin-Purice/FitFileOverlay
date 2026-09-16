using FFMpegCore;
using FFMpegCore.Extensions.Downloader;
using FFMpegCore.Helpers;
using System.IO;
using System.Security.Principal;
using System.Windows;
using Velopack;
using Velopack.Sources;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using SplashScreen = MyWPFApp.Controls.SplashScreen;

namespace FitFileOverlay.Windows;

public partial class MainWindow : INavigationWindow
{
    private IContentDialogService _contentDialogService;

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationViewPageProvider navigationViewPageProvider,
        INavigationService navigationService,
        IContentDialogService contentDialogService
    )
    {
        ViewModel = viewModel;
        DataContext = this;

        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(navigationViewPageProvider);

        navigationService.SetNavigationControl(RootNavigation);
        contentDialogService.SetDialogHost(RootContentDialog);
        _contentDialogService = contentDialogService;

        GlobalFFOptions.Current.BinaryFolder = Path.Combine(App.SaveDirrectory, "ffbin");
        InitializeSplashScreen();
    }

    public MainWindowViewModel ViewModel { get; }

    #region Splash screen

    private void InitializeSplashScreen()
    {
        List<Func<Action<string>, Task>> tasks =
        [
            UpdateMyApp,
            CheckFFMpegInstall
        ];
        SplashScreen splashScreen = new(tasks);
        SplashScreenHost.Content = splashScreen;
        Task.Run(() => splashScreen.RunTasksAndHideAsync());
    }

    private async Task CheckFFMpegInstall(Action<string> messageChangeCallback)
    {
        messageChangeCallback("Checking if ffmpeg is installed");
        try
        {
            FFMpegHelper.VerifyFFMpegExists(GlobalFFOptions.Current);
        }
        catch
        {
            messageChangeCallback("ffmpeg not found");
            // ffmpeg was not found
            if (IsAdministrator())
            {
                try
                {
                    messageChangeCallback("Downloading ffmpeg binaries");
                    // create bin folder
                    string dirPath = GlobalFFOptions.Current.BinaryFolder;
                    Directory.CreateDirectory(dirPath);
                    // download ffmpeg binaries
                    Task<List<string>> downloadTask = FFMpegDownloader.DownloadBinaries();
                    Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    Task completed = await Task.WhenAny(downloadTask, timeoutTask);
                    if (completed == timeoutTask || completed.IsFaulted)
                        throw new Exception("Time out or error when downloading");
                }
                catch
                {
                    // could not install
                    messageChangeCallback("An error occured while trying to download ffmpeg. Exiting");
                    await Task.Delay(2000);
                    ShutdownApp();
                }
            }
            else
            {
                //need admin permission to install
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    ContentDialog dialog = new()
                    {
                        Title = "FFMpeg not installed",
                        Content = "This app needs ffmpeg to run.\nInstall ffmpeg manually and restart application.\n\nOr restart with admin privileges to install automatically.",
                        IsPrimaryButtonEnabled = false,
                        CloseButtonText = "Exit"
                    };
                    _ = await _contentDialogService.ShowAsync(dialog, default);
                });
                ShutdownApp();
            }
        }
    }

    private async Task UpdateMyApp(Action<string> messageChangeCallback)
    {
        try
        {
            messageChangeCallback("Checking for updates");
            IUpdateSource updateSource = new GithubSource("https://github.com/Florin-Purice/FitFileOverlay", accessToken: null, prerelease: false);
            UpdateManager mgr = new(updateSource);
            // check for new version
            Task<UpdateInfo?> checkTask = mgr.CheckForUpdatesAsync();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(checkTask, timeoutTask);
            if (completed == timeoutTask || completed.IsFaulted)
                return; // Timed out or error (like no internet connection)
            UpdateInfo? newVersion = await checkTask;
            if (newVersion == null)
                return; // no update available
            messageChangeCallback($"Update found: {newVersion.TargetFullRelease.Version.ToFullString()}");
            // ask for update confirmation
            ContentDialogResult? dialogResult = null;
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                ContentDialog dialog = new()
                {
                    Title = $"New version: {newVersion.TargetFullRelease.Version.ToFullString()}",
                    Content = "Download and install now?",
                    PrimaryButtonText = "Yes",
                    CloseButtonText = "Postpone"
                };
                dialogResult = await _contentDialogService.ShowAsync(dialog, default);
            });
            if (dialogResult == ContentDialogResult.Primary)
            {
                messageChangeCallback("Downloading update");
                // download new version
                await mgr.DownloadUpdatesAsync(newVersion);
                // install new version and restart app
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
        }
        catch { }
    }

    private static bool IsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void ShutdownApp()
    {
        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    #endregion

    #region INavigationWindow methods

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    #endregion INavigationWindow methods

    /// <summary>
    /// Raises the closed event.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Make sure that closing this window will begin the process of closing the application.
        Application.Current.Shutdown();
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
