using System.Windows;
using System.Windows.Controls;

namespace TournamentTool.Components.Controls;

public partial class HamburgerMenuItem : RadioButton
{
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(HamburgerMenuItem), new PropertyMetadata(string.Empty));


    public HamburgerMenuItem()
    {
        InitializeComponent();
    }
}
