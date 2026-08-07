using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminFitFilePaceOverlay.Navigation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GarminFitFilePaceOverlay.Pages
{
    internal partial class SettingsPageViewModel : ViewModelBase
    {
        private INavigationService homePageNavigationService;

        [ObservableProperty]
        private bool useFileLTHR;
        [ObservableProperty]
        private string customLTHR;
        [ObservableProperty]
        private uint _FPS;

        public SettingsPageViewModel(INavigationService homePageNavigationService)
        {
            this.homePageNavigationService = homePageNavigationService;
            useFileLTHR = Settings.Get<bool>("UseFileLTHR");
            _FPS = Settings.Get<uint>("FPS");
            int customLthrValue = Settings.Get<int>("CustomLTHR");
            customLTHR = customLthrValue.ToString();
        }

        [RelayCommand]
        private void NavigateToHomePage() => homePageNavigationService.Navigate();

        partial void OnUseFileLTHRChanged(bool value)
        {
            Settings.Set("UseFileLTHR", value);
        }

        partial void OnCustomLTHRChanged(string value)
        {
            if (int.TryParse(value, out int parsedValue))
            {
                Settings.Set("CustomLTHR", parsedValue);
            }
        }

        partial void OnFPSChanged(uint value)
        {
            Settings.Set("FPS", value);
        }
    }
}
