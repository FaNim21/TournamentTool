using System.Windows;
using TournamentTool.Properties;

namespace TournamentTool.App.Windows;

public partial class DebugWindow : Window
{
    public DebugWindow()
    {
        InitializeComponent();

        if (Settings.Default.DebugWindowTop == -1 && Settings.Default.DebugWindowLeft == -1)
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Settings.Default.DebugWindowLeft;
            Top = Settings.Default.DebugWindowTop;
        }
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        Settings.Default.DebugWindowLeft = Left;
        Settings.Default.DebugWindowTop = Top;

        Settings.Default.Save();
        base.OnClosed(e);
    }
}
