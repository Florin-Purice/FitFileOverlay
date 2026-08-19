namespace FitFileOverlay.Navigation;

public interface INavigationManager
{
    /// <summary>
    /// Holds the value of the active viewmodel
    /// </summary>
    public INavigableViewModel CurrentViewModel { get; }

    /// <summary>
    /// Will try to navigate using a registered viewmodel factory for given <b>NavigationTarget</b><br/>
    /// <b>CurrentViewModel</b> will change to reflect the navigation result
    /// </summary>
    /// <param name="navigationTarget">The type the desired viewmodel factory has been registered to</param>
    /// <returns><b>true</b> if navigation was successful, <b>false</b> if no viewmodel factory has been registered for the specified <b>NavigationTarget</b></returns>
    public bool NavigateTo(NavigationTarget navigationTarget);

    /// <summary>
    /// Registered a viewmodel factory for a specified <b>NavigationTarget</b><br/>
    /// Subsequent registrations for the same <b>NavigationTarget</b> will overwrite the previous one
    /// </summary>
    /// <param name="navigationTarget"></param>
    /// <param name="navigableViewModelFactory"></param>
    public void RegisterViewModelFactory(NavigationTarget navigationTarget, Func<INavigableViewModel> navigableViewModelFactory);
}
