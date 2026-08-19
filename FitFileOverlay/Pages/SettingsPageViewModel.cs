using CommunityToolkit.Mvvm.ComponentModel;

using FitFileOverlay.Navigation;

namespace FitFileOverlay.Pages;

public partial class SettingsPageViewModel : ViewModelBase, INavigableViewModel
{
    [ObservableProperty]
    public partial string PageLabel { get; set; } = "Settings page";
}
