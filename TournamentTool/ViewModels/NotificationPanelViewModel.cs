using System.Collections.ObjectModel;
using System.Runtime.InteropServices.JavaScript;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Commands.Controller;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;
using ZLinq;

namespace TournamentTool.ViewModels;

public class NotificationPanelViewModel : BaseViewModel
{
    private readonly LogStore _store;

    public event EventHandler? PanelOpened;

    public ObservableCollection<LogEntryViewModel> Notifications { get; private set; } = [];

    private bool _isNotificationPanelOpen;
    public bool IsNotificationPanelOpen
    {
        get => _isNotificationPanelOpen;
        set
        {
            if (_isNotificationPanelOpen == value) return;

            _isNotificationPanelOpen = value;
            OnPropertyChanged(nameof(IsNotificationPanelOpen));
        }
    }
    
    private const int _maxNotifications = 50;
    private const LogLevel _minimumLevel = LogLevel.Info;
    private DateTime _lastClearedTimestamp = DateTime.MinValue;
    private bool _isOpened;

    public event EventHandler<LogEntry>? NotificationReceived;
    
    public ICommand HidePanelCommand { get; private set; }
    public ICommand ClearPanelCommand { get; private set; }


    public NotificationPanelViewModel(LogStore store)
    {
        _store = store;
        _store.LogReceived += OnLiveLogReceived;
        
        HidePanelCommand = new RelayCommand(HidePanel);
        ClearPanelCommand = new RelayCommand(Clear);
    }
    public override void Dispose()
    {
        _store.LogReceived -= OnLiveLogReceived;
    }

    public override void OnEnable(object? parameter)
    {
        _isOpened = true;
        var logs = _store.Logs.AsValueEnumerable()
            .Where(e => e.Level >= _minimumLevel)
            .Where(e => e.Date > _lastClearedTimestamp)
            .TakeLast(_maxNotifications)
            .Select(e => new LogEntryViewModel(e)).ToList();
        
        foreach (var logsChunk in logs.Batch(5))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var log in logsChunk)
                {
                    Notifications.Add(log);
                }
            }, DispatcherPriority.Background);
        }
    }
    public override bool OnDisable()
    {
        _isOpened = false;
        Application.Current.Dispatcher.Invoke(() =>
        {
            Notifications.Clear();
        });
        return true;
    }

    public void ShowPanel()
    {
        PanelOpened?.Invoke(this, EventArgs.Empty);
        OnEnable(null);
        IsNotificationPanelOpen = true;
    }
    public void HidePanel()
    {
        OnDisable();
        IsNotificationPanelOpen = false;
    }

    public void OnLiveLogReceived(object? sender, LogEntry log)
    {
        if (log.Level < _minimumLevel) return;
        if (_isOpened)
        {
            Application.Current.Dispatcher.Invoke(() => Notifications.Add(new LogEntryViewModel(log)));
            return;
        }

        NotificationReceived?.Invoke(this, log);
    }
    
    private void Clear()
    {
        var option = DialogBox.Show("Are you sure you want to clear the notification panel?", "Clearing notification panel", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (option != MessageBoxResult.Yes) return;
        
        _lastClearedTimestamp = DateTime.Now;
        Application.Current.Dispatcher.Invoke(() => Notifications.Clear());
    }
}