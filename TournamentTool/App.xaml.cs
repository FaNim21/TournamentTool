using System.Windows;
using TournamentTool.ViewModels;

namespace TournamentTool;

public partial class App : Application
{
    public MainViewModel MainViewModel { get; set; }

    public App() { }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow window = new();

        MainViewModel = new MainViewModel();
        window.DataContext = MainViewModel;
        window.Show();

        MainWindow = window;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
