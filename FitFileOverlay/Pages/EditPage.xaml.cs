using Wpf.Ui.Abstractions.Controls;

namespace FitFileOverlay.Pages;

/// <summary>
/// Interaction logic for EditPage.xaml
/// </summary>
public partial class EditPage : INavigableView<EditPageViewModel>
{
    public EditPageViewModel ViewModel { get; }

    public EditPage(EditPageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
    }
}
