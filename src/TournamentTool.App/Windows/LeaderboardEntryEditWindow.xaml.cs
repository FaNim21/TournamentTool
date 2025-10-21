using System.ComponentModel;
using System.Windows;
using TournamentTool.Core.Common;

namespace TournamentTool.App.Windows;

public partial class LeaderboardEntryEditWindow : Window
{
    public LeaderboardEntryEditWindow()
    {
        InitializeComponent();
    }
    
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (!((BaseViewModel)DataContext).OnDisable()) return;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
}