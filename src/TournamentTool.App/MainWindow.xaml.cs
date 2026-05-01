using System.ComponentModel;
using System.Windows;
using TournamentTool.Core.Utils;
using TournamentTool.Properties;
using TournamentTool.ViewModels;

namespace TournamentTool.App;

public partial class MainWindow : Window
{
    private static Mutex? _mutex;
    private const string MutexName = "TournamentTool_mutex";
    
    private bool _handledCrash = false;


    public MainWindow()
    {
        if (!IsSingleInstance())
        {
            MessageBox.Show("Existing instance of Tournament Tool is running");
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

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
        };

        Application.Current.DispatcherUnhandledException += (_, e) =>
        {
            LogUnhandledExceptionAsNotCrashed(e.Exception, "Application.Current.DispatcherUnhandledException");
            e.Handled = true; //Problem z tym, ze to blokuje wywalanie aplikacji, a nie wiem czy to sie do czegos ma jak i tak nie ma UI xd
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogUnhandledExceptionAsNotCrashed(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }
    
    private static bool IsSingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
    }

    private void LogUnhandledExceptionAsNotCrashed(Exception exception, string source)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        
        mainViewModel.Logger.Error($"({source}) {exception}");
    }
    private void LogUnhandledException(Exception exception, string source)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.SaveAll();
            try
            {
                mainViewModel.ShowUnhandledExceptionLog(exception.Message);
            }
            catch
            {
                // ignored
            }

            Task.Run(async () => await mainViewModel.LogStore.SaveToFileAsync());
        }
        
        string output = $"UnhandledException ({source}): {exception}";
        Helper.SaveLog(output, "crash_log");
    }

    private void MinimizeButtonsClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel mainViewModel) return;
        
        if (!mainViewModel.TournamentState.IsModified)
        {
            mainViewModel.OnClose();
            Close();
            return;
        }

        if (!mainViewModel.OnDisable()) return;
        
        mainViewModel.OnClose();
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
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