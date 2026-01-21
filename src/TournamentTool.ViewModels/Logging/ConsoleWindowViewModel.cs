using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.ViewModels.Commands;

namespace TournamentTool.ViewModels.Logging;

public class ConsoleWindowViewModel : BaseWindowViewModel
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IWindowService _windowService;
    public ConsoleViewModel Console { get; init; }
    
    public ICommand BackToMainWindowCommand { get; private set; }
    public ICommand HideWindowCommand { get; private set; }

    
    public ConsoleWindowViewModel(ConsoleViewModel console, ISettingsProvider settingsProvider, IWindowService windowService, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        Console = console;
        _settingsProvider = settingsProvider;
        _windowService = windowService;
        
        BackToMainWindowCommand = new RelayCommand(BackToMainWindow);
        HideWindowCommand = new RelayCommand(HideWindow);
    }
    
    private void BackToMainWindow()
    {
        _settingsProvider.Get<AppCache>().IsConsoleWindowed = false;
        
        Console.IsDockedConsoleVisible = true;
        Console.IsWindowed = false;
        _windowService.Close<ConsoleWindowViewModel>();
    }

    private void HideWindow()
    {
        Console.IsOpen = false;
        
        _windowService.Hide<ConsoleWindowViewModel>();
        _windowService.FocusMainWindow();
    }
}