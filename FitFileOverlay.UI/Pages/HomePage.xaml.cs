using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Abstractions.Controls;

namespace FitFileOverlay.UI.Pages;

public partial class HomePage : INavigableView<HomePageViewModel>
{
    public HomePage(HomePageViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public HomePageViewModel ViewModel { get; }

    private async void Page_DropAsync(object sender, DragEventArgs e)
    {
        // Wrap logic in Dispatcher.InvokeAsync to prevent blocking
        await Dispatcher.InvokeAsync(async () =>
        {
            ViewModel.IsFileDragOver = false;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                await ViewModel.LoadFile(files[0]);

            e.Handled = true;
        });
    }

    private void Page_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            ViewModel.IsFileDragOver = true;
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            ViewModel.IsFileDragOver = false;
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void DropArea_DragLeave(object sender, DragEventArgs e)
    {
        ViewModel.IsFileDragOver = false;
        e.Handled = true;

    }
}
