using CommunityToolkit.Mvvm.Messaging;
using FitFileOverlay.Enums;
using FitFileOverlay.Helpers;
using FitFileOverlay.Models;
using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace FitFileOverlay.Pages;

public partial class SettingsViewModel : ObservableObject
{
    private readonly UpdateManager? _updataManager;
    private readonly IMessenger _messenger;
    private UpdateInfo? _updateInfo;

    public SettingsViewModel(IMessenger messenger)
    {
        _messenger = messenger;

        AppThemeValues = Enum.GetValues<AppTheme>();
        AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? string.Empty;
        AppSettings = App.AppSettings;
        AppSettings.PropertyChanged += OnAppSettingsPropertyChanged;

        try
        {
            IUpdateSource updateSource = new GithubSource("https://github.com/Florin-Purice/FitFileOverlay", accessToken: null, prerelease: false);
            _updataManager = new(updateSource);
        }
        catch { }
        Task.Run(() => SearchForUpdates());
    }

    [ObservableProperty]
    public partial AppSettings AppSettings { get; private set; }
    [ObservableProperty]
    public partial AppTheme[] AppThemeValues { get; private set; }
    [ObservableProperty]
    public partial string AppVersion { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string UpdateVersion { get; set; } = string.Empty;
    [ObservableProperty]
    public partial UpdateStatus UpdateStatus { get; set; } = UpdateStatus.Searching;

    [RelayCommand]
    private async Task SearchForUpdates()
    {
        try
        {
            UpdateStatus = UpdateStatus.Searching;
            _updateInfo = await CheckForUpdatesAsync();
            if (_updateInfo == null)
            {
                UpdateStatus = UpdateStatus.UpToDate;
                return;
            }
            UpdateStatus = UpdateStatus.Available;
            UpdateVersion = _updateInfo.TargetFullRelease.Version.ToFullString();
        }
        catch
        {
            UpdateStatus = UpdateStatus.SearchError;
        }
    }

    [RelayCommand]
    private async Task InstallUpdates()
    {
        try
        {
            UpdateStatus = UpdateStatus.Updating;
            _messenger.Send(new CanNavigateMessage(false));
            await InstallUpdatesAndRestart();
        }
        catch 
        {
            UpdateStatus = UpdateStatus.InstallError;
            _messenger.Send(new CanNavigateMessage(true));
        }
    }

    private async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        if (_updataManager == null)
            throw new NullReferenceException("UpdateManager is null");
        Task<UpdateInfo?> checkTask = _updataManager.CheckForUpdatesAsync();
        int delaySeconds = 10;
        Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        Task completed = await Task.WhenAny(checkTask, timeoutTask);
        if (completed == timeoutTask || completed.IsFaulted)
            throw new TimeoutException($"Opperation timed out after {delaySeconds}");
        return await checkTask;
    }

    private async Task InstallUpdatesAndRestart()
    {
        if (_updataManager == null || _updateInfo == null)
            throw new NullReferenceException();
        await _updataManager.DownloadUpdatesAsync(_updateInfo);
        _updataManager.ApplyUpdatesAndRestart(_updateInfo);
    }

    private void OnAppSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.Theme))
            App.ChangeTheme(AppSettings.Theme);
    }
}
