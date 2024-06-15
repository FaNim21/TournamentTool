using System.Windows;
using System.Windows.Controls;

namespace TournamentTool.Components.Controls;

public partial class HamburgerMenu : UserControl
{
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }
    public object HamburgerContent
    {
        get => GetValue(HamburgerContentProperty);
        set => SetValue(HamburgerContentProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register("IsOpen", typeof(bool), typeof(HamburgerMenu), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty HamburgerContentProperty = DependencyProperty.Register("HamburgerContent", typeof(object), typeof(HamburgerMenu), new PropertyMetadata(null));


    public HamburgerMenu()
    {
        InitializeComponent();
    }

    private void BorderMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        IsOpen = false;
    }
}
