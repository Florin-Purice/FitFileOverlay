using CommunityToolkit.Mvvm.ComponentModel;

using FitFileOverlay.Navigation;

namespace FitFileOverlay.Pages;

public partial class HomePageViewModel : ViewModelBase, INavigableViewModel
{
    [ObservableProperty]
    public partial string PageLabel { get; set; } = "Home page";
}
