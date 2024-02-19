using System.Windows;
using System.Windows.Input;
using TournamentTool.Utils;

namespace TournamentTool.Windows;

public partial class SceneManagerWindow : Window
{
    public SceneManagerWindow()
    {
        InitializeComponent();

        labelVersion.Content = Consts.Version;
    }

    private void HeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }
    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        Application.Current.MainWindow.Show();
        Close();
    }
}
