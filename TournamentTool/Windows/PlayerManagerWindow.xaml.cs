using System.Windows;
using System.Windows.Input;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Windows;

public partial class PlayerManagerWindow : Window
{
    public PlayerManagerWindow()
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
        if (DataContext is PlayerManagerViewModel playerView && playerView.IsEditing)
        {
            MessageBox.Show("Finish editing before closing the window", "Editing");
            return;
        }
        Application.Current.MainWindow.Show();
        Close();
    }
}
