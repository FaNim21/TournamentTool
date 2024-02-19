using System.Windows;
using System.Windows.Input;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Windows;

public partial class ControllerWindow : Window
{
    public ControllerWindow()
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
        if (DataContext is ControllerViewModel viewmodel)
            viewmodel.ControllerExit();

        Application.Current.MainWindow.Show();
        Close();
    }
}
