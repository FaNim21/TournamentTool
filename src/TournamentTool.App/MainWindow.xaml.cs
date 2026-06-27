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
    }
    
    private static bool IsSingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
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