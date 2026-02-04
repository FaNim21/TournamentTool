using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities;
using ZLinq;

namespace TournamentTool.ViewModels;

public class NotificationPanelViewModel : BaseViewModel
{
    private readonly ILogStore _store;
    private readonly IDialogService _dialogService;
    private readonly IDispatcherService _dispatcher;
    private readonly IClipboardService _clipboard;

    public event EventHandler? PanelOpened;

    public ObservableCollection<LogEntryViewModel> Notifications { get; } = [];
    private readonly Dictionary<(LogLevel level, string message), LogEntryViewModel> _notificationsLookup = [];
    
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
    
    private const int _maxNotifications = 40;
    private const int _maxEvaluatedLogs = 100;
    private const LogLevel _minimumLevel = LogLevel.Info;
    private DateTime _lastClearedTimestamp = DateTime.MinValue;

    public event EventHandler<LogEntry>? NotificationReceived;
    
    public ICommand HidePanelCommand { get; private set; }
    public ICommand ClearPanelCommand { get; private set; }
    public ICommand CopyNotificationToClipboardCommand { get; private set; }


    public NotificationPanelViewModel(ILogStore store, IDialogService dialogService, IDispatcherService dispatcher, IClipboardService clipboard) : base(dispatcher)
    {
        _store = store;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
        _clipboard = clipboard;
        _store.LogReceived += OnLiveLogReceived;
        
        HidePanelCommand = new RelayCommand(HidePanel);
        ClearPanelCommand = new RelayCommand(Clear);
        CopyNotificationToClipboardCommand = new RelayCommand<LogEntryViewModel>(CopyNotificationToClipboard);
    }
    public override void Dispose()
    {
        _store.LogReceived -= OnLiveLogReceived;
    }

    public override void OnEnable(object? parameter)
    {
        IsOpen = true;
        
        var logs = _store.Logs.AsValueEnumerable()
            .Where(e => e.Level >= _minimumLevel)
            .Where(e => e.Date > _lastClearedTimestamp)
            .TakeLast(_maxEvaluatedLogs)
            .ToList();
        
        foreach (var logsChunk in logs.Batch(10))
        {
            _dispatcher.Invoke(() =>
            {
                foreach (var log in logsChunk)
                {
                    AddOrUpdate(log);
                }
            }, CustomDispatcherPriority.Background);
        }
    }
    public override bool OnDisable()
    {
        IsOpen = false;
        
        _dispatcher.Invoke(() =>
        {
            Notifications.Clear();
            _notificationsLookup.Clear();
        });
        return true;
    }

    public void ShowPanel()
    {
        PanelOpened?.Invoke(this, EventArgs.Empty);
        OnEnable(null);
    }
    public void HidePanel()
    {
        OnDisable();
    }

    private void AddOrUpdate(LogEntry entry)
    {
        var key = (entry.Level, entry.Message);

        if (_notificationsLookup.TryGetValue(key, out LogEntryViewModel? existing))
        {
            existing.Amount++;
            
            int index = Notifications.IndexOf(existing);
            Notifications.Move(index, 0);
            return;
        }

        if (Notifications.Count >= _maxNotifications)
        {
            LogEntryViewModel oldestLog = Notifications[0];
            _notificationsLookup.Remove((oldestLog.Level, oldestLog.Message));
            Notifications.RemoveAt(0);
        }

        LogEntryViewModel log = new(entry, _dispatcher);

        _notificationsLookup[key] = log;
        Notifications.Insert(0, log);
    }
    
    public void OnLiveLogReceived(object? sender, LogEntry log)
    {
        if (log.Level < _minimumLevel) return;
        if (IsOpen)
        {
            _dispatcher.Invoke(() => { AddOrUpdate(log); });
            return;
        }

        NotificationReceived?.Invoke(this, log);
    }
    
    private void CopyNotificationToClipboard(LogEntryViewModel log)
    {
        _clipboard.SetText($"[{log.Level}] {log.Message}");
    }
    
    private void Clear()
    {
        var option = _dialogService.Show("Are you sure you want to clear the notification panel?", "Clearing notification panel", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (option != MessageBoxResult.Yes) return;
        
        _lastClearedTimestamp = DateTime.Now;
        _dispatcher.Invoke(() =>
        {
            Notifications.Clear();
            _notificationsLookup.Clear();
        });
    }
}