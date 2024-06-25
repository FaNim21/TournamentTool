using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (Settings.Default.MainWindowLeft == -1 && Settings.Default.MainWindowTop == -1)
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = Settings.Default.MainWindowLeft;
            Top = Settings.Default.MainWindowTop;
        }
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        if (mainViewModel.SelectedViewModel != null && mainViewModel.SelectedViewModel is not PresetManagerViewModel)
        {
            var option = DialogBox.Show($"Are you sure you want to exit from here?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (option == MessageBoxResult.Yes)
            {
                if (mainViewModel.SelectedViewModel != null && !mainViewModel.SelectedViewModel.OnDisable()) return;
                Close();
            }
            return;
        }

        if (mainViewModel.SelectedViewModel != null && !mainViewModel.SelectedViewModel.OnDisable()) return;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        InputController.Instance.Cleanup();
        base.OnClosing(e);
    }
    private void OnClosed(object sender, EventArgs e)
    {
        Settings.Default.MainWindowLeft = Left;
        Settings.Default.MainWindowTop = Top;

        Settings.Default.Save();
        Application.Current.Shutdown();
    }

}