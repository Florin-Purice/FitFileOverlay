using System.Windows;
using System.Windows.Controls;

namespace MyWPFApp.Controls;

/// <summary>
/// Interaction logic for SplashScreen.xaml
/// </summary>
[ObservableObject]
public partial class SplashScreen : UserControl
{
    private readonly List<Func<Action<string>, Task>> _tasks;

    public SplashScreen(List<Func<Action<string>, Task>> taskWithMessageUpdateCallbackList)
    {
        _tasks = taskWithMessageUpdateCallbackList;
        DataContext = this;
        InitializeComponent();
    }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    public async Task RunTasksAndHideAsync()
    {
        foreach (Func<Action<string>, Task> task in _tasks)
            await task.Invoke(UpdateMessage);
        Collapse();
    }

    private void UpdateMessage(string newMessage)
    {
        Message = newMessage;
    }

    private void Collapse()
    {
        Dispatcher?.Invoke(() => Visibility = Visibility.Collapsed);
    }
}
