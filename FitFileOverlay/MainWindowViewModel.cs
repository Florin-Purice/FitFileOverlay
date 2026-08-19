using CommunityToolkit.Mvvm.Input;

using FitFileOverlay.Navigation;

namespace FitFileOverlay;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(INavigationManager navigationManager)
    {
        NavigationManager = navigationManager;
    }

    public INavigationManager NavigationManager { get; private set; }

    [RelayCommand]
    public void NavigateToHome() => NavigationManager.NavigateTo(NavigationTarget.Home);

    [RelayCommand]
    public void NavigateToSettings() => NavigationManager.NavigateTo(NavigationTarget.Settings);
}
