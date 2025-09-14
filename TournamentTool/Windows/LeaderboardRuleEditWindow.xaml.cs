using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Windows;

public partial class LeaderboardRuleEditWindow : Window
{
    public LeaderboardRuleEditWindow()
    {
        InitializeComponent();
        
        InputController.Instance.InitializeNewWindow(this);
    }
    
    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        ((IDisposable)DataContext).Dispose();
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        InputController.Instance.CleanupWindow(this);
        base.OnClosed(e);
    }

}