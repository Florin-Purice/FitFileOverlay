using CommunityToolkit.Mvvm.Messaging;
using FitFileOverlay.Helpers;
using FitFileOverlay.Pages;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui.TaskBar;

namespace FitFileOverlay.Windows;

public partial class MainWindowViewModel : ObservableObject, IRecipient<CanNavigateMessage>, IRecipient<TaskBarProgressMessage>
{
    public MainWindowViewModel(IMessenger messenger)
    {
        messenger.Register<CanNavigateMessage>(this);
        messenger.Register<TaskBarProgressMessage>(this);
    }

    public Window? ParentWindow { get; set; }

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
        },
        new NavigationViewItem(){
            Content = "Crop",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Crop24 },
            TargetPageType = typeof(CropPage)
        },
        new NavigationViewItem(){
            Content = "Edit",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Edit24 },
            TargetPageType = typeof(EditPage)
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

    public void Receive(TaskBarProgressMessage message)
    {
        int current = (int)(message.ProgressValue * 100);
        if (current < 0) 
            current = 0;
        if (current > 100) 
            current = 100;
        ParentWindow?.Dispatcher?.Invoke(() => TaskBarProgress.SetValue(ParentWindow, message.ProgressState, current, 100));
    }
}
