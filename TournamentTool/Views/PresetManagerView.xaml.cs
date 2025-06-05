using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using TournamentTool.Utils;

namespace TournamentTool.Views;

public partial class PresetManagerView : UserControl
{
    public PresetManagerView()
    {
        InitializeComponent();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = RegexPatterns.NumbersPattern();
        e.Handled = regex.IsMatch(e.Text);
    }

    private void OpenDonateSite(object sender, RequestNavigateEventArgs e)
    {
        Helper.StartProcess(e.Uri.ToString());
    }
    private void OpenDonateSiteButton(object sender, RoutedEventArgs e)
    {
        Helper.StartProcess("https://ko-fi.com/fanim");
    }
}
