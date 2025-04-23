using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using TournamentTool.Components.Controls;
using TournamentTool.Utils;

namespace TournamentTool.Views;

public partial class UpdatesView : UserControl
{
    public UpdatesView()
    {
        InitializeComponent();
    }

    private void OpenVersionsSite(object sender, RequestNavigateEventArgs e)
    {
        if (DialogBox.Show($"Do you want to open TournamentTool site to check for new updates or patch notes?",
                $"Opening Github Release site For TournamentTool", MessageBoxButton.YesNo,
                MessageBoxImage.Information) != MessageBoxResult.Yes) return;
        
        Helper.StartProcess(e.Uri.ToString());
    }
}
