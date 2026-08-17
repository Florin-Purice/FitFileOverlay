using System;
using System.Collections.Generic;
using System.Text;

namespace GarminFitFilePaceOverlay.Navigation
{
    public interface INavigationManager
    {
        void Navigate(NavigationTarget navigateTo);
        void Register(NavigationTarget navigationTarget, INavigationService navigationService);
    }

    public class NavigationManager : INavigationManager
    {
        private readonly Dictionary<NavigationTarget, INavigationService> registeredNavs = [];

        public void Navigate(NavigationTarget navigateTo)
        {
            if (registeredNavs.TryGetValue(navigateTo, out INavigationService? value))
               value.Navigate();
        }

        public void Register(NavigationTarget navigationTarget, INavigationService navigationService) => registeredNavs[navigationTarget] = navigationService;
    }

    public enum NavigationTarget
    {
        HomePage,
        SettingsPage
    }
}
