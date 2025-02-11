using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TournamentTool.Views;
public partial class PlayerManagerView : UserControl
{
    [GeneratedRegex("[^0-9]+")]
    private static partial Regex NumberRegex();

    public PlayerManagerView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = NumberRegex();
        e.Handled = regex.IsMatch(e.Text);
    }
}
