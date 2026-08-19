using CommunityToolkit.Mvvm.ComponentModel;

namespace FitFileOverlay.Navigation;

public partial class SimpleNavigationManager : ObservableObject, INavigationManager
{
    private readonly Dictionary<NavigationTarget, Func<INavigableViewModel>> _factories = [];

    [ObservableProperty]
    public partial INavigableViewModel CurrentViewModel { get; private set; }

    public bool NavigateTo(NavigationTarget navigationTarget)
    {
        if(_factories.TryGetValue(navigationTarget, out Func<INavigableViewModel>? viewModelFactory))
        {
            CurrentViewModel = viewModelFactory();
            return true;
        }
        return false;
    }

    public void RegisterViewModelFactory(NavigationTarget navigationTarget, Func<INavigableViewModel> navigableViewModelFactory)
    {
        _factories[navigationTarget] = navigableViewModelFactory;
    }
}