using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Logging;

public class ConsoleViewModel : BaseViewModel
{
    private const int _MAX_LOGS = 100;
    
    private readonly LogStore _store;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsProvider _settingsProvider;

    public ObservableCollection<LogEntryViewModel> Logs { get; } = [];
    
    private bool _isOpen;
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen == value) return;

            _isOpen = value;
            OnPropertyChanged(nameof(IsOpen));
        }
    }

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPropertyChanged(nameof(IsVisible));
        }
    }

    public ICommand SaveLogsCommand { get; private set; }
    public ICommand ClearLogsCommand { get; private set; }
    
    public ICommand OpenInNewWindowCommand { get; private set; }
    public ICommand BackToMainWindowCommand { get; private set; }

    private bool _isWindowed;
    
    
    public ConsoleViewModel(LogStore store, IDispatcherService dispatcher, IWindowService windowService, IDialogService dialogService,
        ISettingsProvider settingsProvider) : base(dispatcher)
    {
        _store = store;
        _dispatcher = dispatcher;
        _windowService = windowService;
        _dialogService = dialogService;
        _settingsProvider = settingsProvider;

        SaveLogsCommand = new RelayCommand(SaveLogs);
        ClearLogsCommand = new RelayCommand(()=> { ClearLogs(true); });
        
        OpenInNewWindowCommand = new RelayCommand(OpenInNewWindow);
        BackToMainWindowCommand = new RelayCommand(BackToMainWindow);
        
        _isWindowed = _settingsProvider.Get<AppCache>().IsConsoleWindowed;

        _store.LogReceived += OnLiveLogReceived;
    }
    public override void Dispose()
    {
        _store.LogReceived -= OnLiveLogReceived;
        
        ClearLogs();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            IsOpen = false;

            if (_isWindowed)
            {
                CloseWindow();
            }
            else
            {
                IsVisible = false;
            }
            return;
        }

        if (_isWindowed)
        {
            OpenWindow();
        }
        else
        {
            IsVisible = true;
        }
        IsOpen = true;
    }

    private void OpenInNewWindow()
    {
        if (_isWindowed) return;

        _settingsProvider.Get<AppCache>().IsConsoleWindowed = true;
        
        IsVisible = false;
        _isWindowed = true;
        OpenWindow();
    }
    private void BackToMainWindow()
    {
        if (!_isWindowed) return;
        
        _settingsProvider.Get<AppCache>().IsConsoleWindowed = false;
        
        IsVisible = true;
        _isWindowed = false;
        CloseWindow();
    }

    private void SaveLogs()
    {
        MessageBoxResult result = _dialogService.Show("Are you sure you want to save all console logs?\nAfter saving all logs will be removed from console", "Saving all console logs", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        
        //trzeba wywolac zewnetrzna metoda do zapisywania logow i tam tez od razu zrobic czyszczenie zeby zbierac logi na nowo po zapisaniu
        //TODO: 0 Zrobic system zapisywania logow, od razu tez przy wylaczaniu aplikacji, zgodnie z ustawieniami PAMIETAC
    }
    private void ClearLogs(bool confirm = false)
    {
        if (confirm)
        {
            MessageBoxResult result = _dialogService.Show("Are you sure you want to clear all visible here console logs?", "Clearing console logs", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        
        Dispatcher.Invoke(() =>
        {
            Logs.Clear();
        });
    }

    private void CloseWindow()
    {
        //TODO: 0 Chyba jednak lepiej bedzie zrobic kontrolowane okno bez niszczenia, jako zmiane visibility dla okna
        // tez to rozwiazanie bedzie wydajniejsze
        // znaczy mozna na guzikach do dokowania i wyciagania zrobic zeby okno sie tworzylo i niszczylo, ale na toggle trzeba dac zmiane visibility
        // wtedy tez x bedzie odpowiadal za visiblity, a nie za niszczenie
        
        //Problemem jest tez nie przechwytywany zawsze trigger do toggle'a
        //czy tez tylda, ktora czasami nie togglue konsoli w trybie przyklejonym tez
        _windowService.Close<ConsoleWindowViewModel>();
    }
    private void OpenWindow()
    {
        ConsoleWindowViewModel consoleWindowViewModel = new(this, Dispatcher);
        _windowService.Show(consoleWindowViewModel, _ => { IsOpen = false; }, "ConsoleWindow");
    }
    
    private void OnLiveLogReceived(object? sender, LogEntry log)
    {
        if (Logs.Count >= _MAX_LOGS)
        {
            int amountToRemove = Logs.Count - _MAX_LOGS;
            
            _dispatcher.Invoke(() =>
            {
                for (int i = 0; i < amountToRemove; i++)
                {
                    Logs.RemoveAt(0);
                }
            });
        }
        
        _dispatcher.Invoke(() => Logs.Add(new LogEntryViewModel(log, _dispatcher)));
    }
}