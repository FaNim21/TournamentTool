using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Windows;

namespace TournamentTool.Modules;

public class MainViewModelCoordinator : IPresetSaver, ILoadingDialog, IDialogWindow
{
    public MainViewModel MainViewModel { get; }
    
    
    public MainViewModelCoordinator(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public void SavePreset(IPreset? preset = null)
    {
        MainViewModel.SavePreset(preset);
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