using System.Windows.Controls;
using TournamentTool.Commands;
using TournamentTool.Models;

namespace TournamentTool.ViewModels.StatusBar;

public class NotificationStatusViewModel : StatusItemViewModel
{
    private readonly NotificationPanelViewModel _notificationPanel;

    protected override string Name => "Notifications";

    private int _errorsCount;
    public int ErrorsCount
    {
        get => _errorsCount;
        set
        {
            _errorsCount = value;
            OnPropertyChanged(nameof(ErrorsCount));
        }
    }


    public NotificationStatusViewModel(NotificationPanelViewModel notificationPanel)
    {
        _notificationPanel = notificationPanel;
        _notificationPanel.NotificationReceived += OnNotificationReceived;
    }
    public override void Dispose()
    {
        _notificationPanel.NotificationReceived -= OnNotificationReceived;
    }

    protected override void InitializeImages()
    {
        AddStateImage("off", "StatusBarIcons/bell-off.png");
        AddStateImage("on", "StatusBarIcons/bell-on.png");
    }
    protected override void InitializeState()
    {
        Clear();
    }
    
    protected override void BuildContextMenu(ContextMenu menu)
    {
        menu.Items.Clear();

        if (!_notificationPanel.IsNotificationPanelOpen)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "View",
                Command = new RelayCommand(ShowNotifications)
            });
        }
        else
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Close",
                Command = new RelayCommand(_notificationPanel.HidePanel)
            });
        }
        menu.Items.Add(new MenuItem 
        { 
            Header = "Mark as read",
            Command = new RelayCommand(Clear)
        });
    }

    private void OnNotificationReceived(object? sender, LogEntry log)
    {
        SetState("on");
        SetToolTip("Unread notifications");

        if (log.Level != LogLevel.Error) return;
        ErrorsCount++;
        SetBadge(ErrorsCount);
    }

    private void ShowNotifications()
    {
        Clear();
        _notificationPanel.ShowPanel();
    }
    private void Clear()
    {
        ErrorsCount = 0;
        SetBadge(ErrorsCount);
        SetState("off");
        SetToolTip("No new notifications");
    }
}