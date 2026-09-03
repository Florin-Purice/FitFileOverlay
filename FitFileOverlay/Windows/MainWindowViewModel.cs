using CommunityToolkit.Mvvm.Messaging;
using FitFileOverlay.Models;
using FitFileOverlay.Pages;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace FitFileOverlay.Windows;

public partial class MainWindowViewModel : ObservableObject, IRecipient<CanNavigateMessage>
{
    public MainWindowViewModel(IMessenger messenger)
    {
        messenger.Register<CanNavigateMessage>(this);
    }

    [ObservableProperty]
    public partial bool CanNavigate { get; set; } = true;

    [ObservableProperty]
    public partial string ApplicationTitle { get; private set; } = "Fit Overlay";

    [ObservableProperty]
    public partial ObservableCollection<object> MenuItems { get; private set; } =
    [
        new NavigationViewItem()
        {
            Content = "Home",
            Icon = new SymbolIcon { Symbol = SymbolRegular.VideoClip24 },
            TargetPageType = typeof(HomePage)
        }
    ];

    [ObservableProperty]
    public partial ObservableCollection<object> FooterMenuItems { get; private set; } =
    [
        new NavigationViewItem()
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(SettingsPage)
        }
    ];

    [ObservableProperty]
    public partial ObservableCollection<MenuItem> TrayMenuItems { get; private set; } =
    [
        new MenuItem { Header = "Home", Tag = "tray_home" }
    ];

    public void Receive(CanNavigateMessage message)
    {
        CanNavigate = message.CanNavigate;
    }
}
