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
    
    private readonly ILogStore _store;
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

    private bool _isDockedConsoleVisible;
    public bool IsDockedConsoleVisible
    {
        get => _isDockedConsoleVisible;
        set
        {
            _isDockedConsoleVisible = value;
            OnPropertyChanged(nameof(IsDockedConsoleVisible));
        }
    }

    public ICommand SaveLogsCommand { get; private set; }
    public ICommand ClearLogsCommand { get; private set; }
    public ICommand OpenInNewWindowCommand { get; private set; }

    public bool IsWindowed;
    
    
    public ConsoleViewModel(ILogStore store, IDispatcherService dispatcher, IWindowService windowService, IDialogService dialogService,
        ISettingsProvider settingsProvider) : base(dispatcher)
    {
        _store = store;
        _dispatcher = dispatcher;
        _windowService = windowService;
        _dialogService = dialogService;
        _settingsProvider = settingsProvider;

        SaveLogsCommand = new RelayCommand(async ()=> { await SaveLogsAsync(); });
        ClearLogsCommand = new RelayCommand(()=> { ClearLogs(true); });
        
        OpenInNewWindowCommand = new RelayCommand(OpenInNewWindow);
        
        IsWindowed = _settingsProvider.Get<AppCache>().IsConsoleWindowed;

        _store.LogReceived += OnLiveLogReceived;
        _store.LogsCleared += OnLogsCleared;
    }
    public override void Dispose()
    {
        _store.LogReceived -= OnLiveLogReceived;
        _store.LogsCleared -= OnLogsCleared;
        
        ClearLogs();
    }

    public void Toggle()
    {
        if (IsOpen)
        {
            IsOpen = false;

            if (IsWindowed)
            {
                _windowService.Hide<ConsoleWindowViewModel>();
                _windowService.FocusMainWindow();
            }
            else
            {
                IsDockedConsoleVisible = false;
            }
            return;
        }

        if (IsWindowed)
        {
            OpenWindow();
        }
        else
        {
            IsDockedConsoleVisible = true;
        }
        IsOpen = true;
    }

    private void OpenInNewWindow()
    {
        _settingsProvider.Get<AppCache>().IsConsoleWindowed = true;
        
        IsDockedConsoleVisible = false;
        IsWindowed = true;
        OpenWindow();
    }

    private async Task SaveLogsAsync()
    {
        MessageBoxResult result = _dialogService.Show("Are you sure you want to save all console logs?\nAfter saving all logs will be removed from console", "Saving all console logs", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        await _store.SaveToFileAsync();
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

    private void OpenWindow()
    {
        ConsoleWindowViewModel consoleWindowViewModel = new(this, _settingsProvider, _windowService, Dispatcher);
        _windowService.Show(consoleWindowViewModel, null, "ConsoleWindow");
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
    private void OnLogsCleared(object? sender, EventArgs e) => ClearLogs();
}