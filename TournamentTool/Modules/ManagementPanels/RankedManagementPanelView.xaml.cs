using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Modules.ManagementPanels;

public partial class RankedManagementPanelView : UserControl
{
    public RankedManagementPanelView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = RegexPatterns.NumbersPattern();
        e.Handled = regex.IsMatch(e.Text);
    }
}
