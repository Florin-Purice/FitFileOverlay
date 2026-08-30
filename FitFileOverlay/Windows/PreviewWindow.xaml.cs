namespace FitFileOverlay.Windows;

public partial class PreviewWindow
{
    public PreviewWindow(PreviewWindowViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public PreviewWindowViewModel ViewModel { get; }
}
