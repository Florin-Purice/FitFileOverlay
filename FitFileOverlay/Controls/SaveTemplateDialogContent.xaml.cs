using System.Windows;
using System.Windows.Controls;

namespace FitFileOverlay.Controls;

/// <summary>
/// Interaction logic for SaveTemplateDialogContent.xaml
/// </summary>
public partial class SaveTemplateDialogContent : UserControl
{
    public static readonly DependencyProperty FileNameProperty =
        DependencyProperty.Register(nameof(FileName), typeof(string), typeof(SaveTemplateDialogContent), new PropertyMetadata(string.Empty));

    public SaveTemplateDialogContent()
    {
        DataContext = this;
        InitializeComponent();
    }

    public string FileName
    {
        get { return (string)GetValue(FileNameProperty); }
        set { SetValue(FileNameProperty, value); }
    }
}
