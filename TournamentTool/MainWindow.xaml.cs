using System.Windows;
using System.Windows.Input;
using TournamentTool.Properties;
using TournamentTool.Utils;

namespace TournamentTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        labelVersion.Content = Consts.Version;

        if (Settings.Default.MainWindowLeft == -1 && Settings.Default.MainWindowTop == -1)
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Settings.Default.MainWindowLeft;
            Top = Settings.Default.MainWindowTop;
        }
    }

    private void HeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e) => Close();

    private void OnClosed(object sender, EventArgs e)
    {
        Settings.Default.MainWindowLeft = Left;
        Settings.Default.MainWindowTop = Top;

        Settings.Default.Save();
        Application.Current.Shutdown();
    }
}