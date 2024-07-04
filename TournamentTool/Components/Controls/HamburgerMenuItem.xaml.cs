using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    private void HamburgerMenuItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (Command != null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }
}
