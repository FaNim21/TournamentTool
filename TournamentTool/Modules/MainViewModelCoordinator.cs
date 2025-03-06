using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.Windows;

namespace TournamentTool.Modules;

public class MainViewModelCoordinator : ICoordinator
{
    private MainViewModel MainViewModel { get; }

    public bool AvailableNewUpdate => MainViewModel.NewUpdate;

    //Tu trzeba sie pozybc zaleznosci z zapisywaniem presetu i zrobic wszystkie zaleznosci zwiazane z oknami i tez popupami, ktore trzeba zrobic w formie informacyjnej
    //zamiast poszczegolnych okien (typu popup prawy gorny rog okna wyswietlajacy sie chwilowo gdzie po kliknieciu znika z miejsca)

    public MainViewModelCoordinator(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public void SelectViewModel(string viewModelName)
    {
        MainViewModel?.SelectViewModel(viewModelName);
    }

    public void ShowDialog(Window window)
    {
        MainViewModel.BlockWindow();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        MainViewModel.UnBlockWindow();
    }
    
    public void ShowLoading(Func<IProgress<float>, IProgress<string>, CancellationToken, Task> loading)
    {
        LoadingWindow window = new(loading);
        ShowDialog(window);
    }
}