using FitFileOverlay.Models;
using System.Windows;
using System.Windows.Controls;

namespace FitFileOverlay.Controls;

/// <summary>
/// Interaction logic for LoadTemplateDialogContent.xaml
/// </summary>
public partial class LoadTemplateDialogContent : UserControl
{


    public KeyValuePair<string, OverlaySettings> SelectedValue
    {
        get { return (KeyValuePair<string, OverlaySettings>)GetValue(SelectedValueProperty); }
        set { SetValue(SelectedValueProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SelectedValue.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedValueProperty =
        DependencyProperty.Register(nameof(SelectedValue), typeof(KeyValuePair<string, OverlaySettings>), typeof(LoadTemplateDialogContent), new PropertyMetadata(new KeyValuePair<string, OverlaySettings>()));



    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(LoadTemplateDialogContent), new PropertyMetadata(0));

    public static readonly DependencyProperty TemplatesProperty =
        DependencyProperty.Register(nameof(Templates), typeof(Dictionary<string, OverlaySettings>), typeof(LoadTemplateDialogContent), new PropertyMetadata(new Dictionary<string, OverlaySettings>()));

    public LoadTemplateDialogContent()
    {
        DataContext = this;
        InitializeComponent();
    }

    public Dictionary<string, OverlaySettings> Templates
    {
        get { return (Dictionary<string, OverlaySettings>)GetValue(TemplatesProperty); }
        set { SetValue(TemplatesProperty, value); }
    }

    public int SelectedIndex
    {
        get { return (int)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); }
    }
}
