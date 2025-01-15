using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TournamentTool.Components.Controls;

namespace TournamentTool.Views;

public partial class UpdatesView : UserControl
{
    public UpdatesView()
    {
        InitializeComponent();
    }

    private void OpenVersionsSite(object sender, RequestNavigateEventArgs e)
    {
        if (DialogBox.Show($"Do you want to open TournamentTool site to check for new updates or patch notes?", $"Opening Github Release site For TournamentTool", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
        {
            var processStart = new ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(processStart);
        }
    }

    private void OpenDonateSite(object sender, RequestNavigateEventArgs e)
    {
        if (DialogBox.Show($"Do you want to open Ko-fi.com/fanim site?", $"Donate here", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
        {
            var processStart = new ProcessStartInfo(e.Uri.ToString())
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(processStart);
        }
    }
}
