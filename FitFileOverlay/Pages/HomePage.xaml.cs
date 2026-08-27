using Wpf.Ui.Abstractions.Controls;

namespace FitFileOverlay.Pages;

public partial class HomePage : INavigableView<HomePageViewModel>
{
    public HomePage(HomePageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public HomePageViewModel ViewModel { get; }
}
