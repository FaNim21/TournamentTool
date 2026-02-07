using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Menu;

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


    public NotificationStatusViewModel(NotificationPanelViewModel notificationPanel, IDispatcherService dispatcher, IImageService imageService, IMenuService menuService) : base(dispatcher, imageService, menuService)
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
    
    public override ContextMenuViewModel BuildContextMenu()
    {
        var menu = new ContextMenuViewModel(Dispatcher);

        if (!_notificationPanel.IsOpen)
        {
            ShowNotifications();
        }
        else
        {
            _notificationPanel.HidePanel();
        }
        
        return menu;
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