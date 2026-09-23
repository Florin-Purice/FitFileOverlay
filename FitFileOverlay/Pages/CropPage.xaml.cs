using System.Windows.Controls;

namespace FitFileOverlay.Pages;

/// <summary>
/// Interaction logic for CropPage.xaml
/// </summary>
public partial class CropPage : Page
{
    public CropPage(CropPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public CropPageViewModel ViewModel { get; }
}
