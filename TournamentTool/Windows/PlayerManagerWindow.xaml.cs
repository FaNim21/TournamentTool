using System.Windows;
using System.Windows.Input;
using TournamentTool.Components.Controls;
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
            DialogBox.Show("Finish editing before closing the window", "Editing");
            return;
        }

        double mainLeft = ((MainWindow)Application.Current.MainWindow).Width / 2;
        double mainTop = ((MainWindow)Application.Current.MainWindow).Height / 2;

        ((MainWindow)Application.Current.MainWindow).Left = Left + (Width / 2) - mainLeft;
        ((MainWindow)Application.Current.MainWindow).Top = Top + (Height / 2) - mainTop;
        Application.Current.MainWindow.Show();
        Close();
    }
}
