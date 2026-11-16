using Wpf.Ui.Abstractions.Controls;

namespace FitFileOverlay.UI.Pages;

/// <summary>
/// Interaction logic for CropPage.xaml
/// </summary>
public partial class CropPage : INavigableView<CropPageViewModel>
{
    public CropPage(CropPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public CropPageViewModel ViewModel { get; }
}
