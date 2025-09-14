using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Properties;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.Windows;

namespace TournamentTool;

public partial class MainWindow : Window
{
    private static Mutex? _mutex;
    private const string MutexName = "TournamentTool_mutex";
    
    private bool _handledCrash = false;


    public MainWindow()
    {
        if (!IsSingleInstance())
        {
            Application.Current.Shutdown();
            return;
        }
        
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
    
    private static bool IsSingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
    }

    private void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.PresetSaver.SavePreset();
        }
        
        if (_handledCrash) return;
        _handledCrash = true;

        string output = $"UnhandledException: {e.Exception}";

        DialogBox.Show($"Unhandled exception: {e.Exception.Message}", "Application crash", MessageBoxButton.OK, MessageBoxImage.Error);

        Helper.SaveLog(output, "crash_log");
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        if (!mainViewModel.PresetInfo.IsPresetModified)
        {
            Close();
            return;
        }

        MessageBoxResult option = DialogBox.Show($"You have unsaved changes in preset.\n" +
                                                 $"Do you want to save those changes?", "WARNING",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        if (option == MessageBoxResult.Yes)
        {
            mainViewModel.PresetSaver.SavePreset();
        } 
        else if (option == MessageBoxResult.No) { }
        else return;

        mainViewModel.NavigationService.SelectedView.OnDisable();
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        InputController.Instance.Cleanup();
        if (_mutex != null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException) { }
            finally
            {
                _mutex.Dispose();
                _mutex = null;
            }
        }
        
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