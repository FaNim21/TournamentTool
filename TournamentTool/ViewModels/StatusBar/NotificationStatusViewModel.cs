using System.Windows.Controls;
using TournamentTool.Commands;

namespace TournamentTool.ViewModels.StatusBar;

public class NotificationStatusViewModel : StatusItemViewModel
{
    // private readonly INotificationService _notificationService;
    private bool _hasUnreadNotifications;

    protected override string Name => "Notifications";
    /*
    public override string ToolTip => _hasUnreadNotifications 
        ? $"~ unread notifications" 
        : "No new notifications";
        */

    
    //TODO: 0 Zrobic dzialajace powiadomienia
    public NotificationStatusViewModel()
    {
        /*
        _notificationService = notificationService;
        _notificationService.NotificationReceived += OnNotificationReceived;
        _notificationService.NotificationsRead += OnNotificationsRead;
    */
    }

    protected override void InitializeImages()
    {
        AddStateImage("off", "StatusBarIcons/bell-off.png");
        AddStateImage("on", "StatusBarIcons/bell-on.png");
    }
    protected override void InitializeState()
    {
        SetState("off");
    }
    
    protected override void BuildContextMenu(ContextMenu menu)
    {
        
        /*
        AddMenuItem("Connect Twitch APi", new RelayCommand(() => _tournament.IsUsingTwitchAPI = true));
        AddMenuItem("Disconnect Twitch API", new RelayCommand(() => _tournament.IsUsingTwitchAPI = false));
    */
    }

    private void OnNotificationReceived(object? sender, EventArgs e)
    {
        _hasUnreadNotifications = true;
        SetState("on");
        OnPropertyChanged(nameof(ToolTip));
    }

    private void OnNotificationsRead(object? sender, EventArgs e)
    {
        // _hasUnreadNotifications = _notificationService.UnreadCount > 0;
        SetState(_hasUnreadNotifications ? "on" : "off");
        OnPropertyChanged(nameof(ToolTip));
    }

    private void ShowNotifications()
    {
        // Pokaż panel powiadomień
    }
}
