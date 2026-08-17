using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GarminFitFilePaceOverlay.Navigation;

namespace GarminFitFilePaceOverlay.Pages
{
    public partial class SettingsPageViewModel : ViewModelBase
    {
        private readonly INavigationManager navigationManager;

        public SettingsPageViewModel(INavigationManager navigationManager)
        {
            this.navigationManager = navigationManager;
            UseFileLTHR = Settings.Get<bool>("UseFileLTHR");
            FPS = Settings.Get<uint>("FPS");
            int customLthrValue = Settings.Get<int>("CustomLTHR");
            CustomLTHR = customLthrValue.ToString();
        }

        [ObservableProperty]
        public partial bool UseFileLTHR { get; set; }
        [ObservableProperty]
        public partial string CustomLTHR { get; set; }
        [ObservableProperty]
        public partial uint FPS { get; set; }

        [RelayCommand]
        private void NavigateToHomePage() => navigationManager.Navigate(NavigationTarget.HomePage);

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
