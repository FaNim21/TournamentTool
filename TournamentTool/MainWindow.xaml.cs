using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class MainWindow : Window
{
    private bool _handledCrash = false;


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

        Dispatcher.UnhandledException += DispatcherUnhandledException;
    }

    private void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (_handledCrash) return;
        if (!_handledCrash) _handledCrash = true;

        string output = $"UnhandledException: {e.Exception.Message}\n" +
                        $"StackTrace: {e.Exception.StackTrace}";

        //TODO: 4 Nie wiem czy sens jest wyswietlac info podczas crasha?
        DialogBox.Show($"Unhandled exception: {e.Exception.Message}", "Application crash", MessageBoxButton.OK, MessageBoxImage.Error);

        Helper.SaveLog(output, "crash_log");
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;

        if (mainViewModel.NavigationService.SelectedView != null && mainViewModel.NavigationService.SelectedView is not PresetManagerViewModel)
        {
            var option = DialogBox.Show($"Are you sure you want to exit from here?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (option == MessageBoxResult.Yes)
            {
                if (mainViewModel.NavigationService.SelectedView != null && !mainViewModel.NavigationService.SelectedView.OnDisable()) return;
                Close();
            }
            return;
        }

        if (mainViewModel.NavigationService.SelectedView != null && !mainViewModel.NavigationService.SelectedView.OnDisable()) return;
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