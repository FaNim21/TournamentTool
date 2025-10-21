using TournamentTool.Core.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Controllers;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public class TwitchStatusViewModel : StatusItemViewModel
{
    private readonly TwitchService _twitchService;

    protected override string Name => "Twitch";
    
    private bool IsConnected => _twitchService.IsConnected;
    private ConnectionState _currentState;

    
    public TwitchStatusViewModel(TwitchService twitchService, IDispatcherService dispatcher, IImageService imageService, IMenuService menuService) : base(dispatcher, imageService, menuService)
    {
        _twitchService = twitchService;
        
        _twitchService.ConnectionStateChanged += OnConnectionStateChanged;
    }
    public override void Dispose()
    {
        _twitchService.ConnectionStateChanged -= OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        _currentState = e.NewState;
        
        switch (e.NewState)
        {
            case ConnectionState.Connected:
                SetState("on");
                break;
            case ConnectionState.Disconnected:
                SetState("off");
                break;
            case ConnectionState.Connecting:
            case ConnectionState.Disconnecting:
                SetState("wait");
                break;
        }

        if (_twitchService.TokenExpiresAt == null)
        {
            SetToolTip(e.NewState.ToString());
            return;
        }
        
        SetToolTip($"{e.NewState}\nExpires: {_twitchService.TokenExpiresAt}");
    }

    protected override void InitializeImages()
    {
        AddStateImage("off", "StatusBarIcons/twitch-off.png");
        AddStateImage("wait", "StatusBarIcons/twitch-wait.png");
        AddStateImage("on", "StatusBarIcons/twitch-on.png");
    }
    protected override void InitializeState()
    {
        SetState("off");
        SetToolTip("Disconnected");
    }

    public override ContextMenuViewModel BuildContextMenu()
    {
        var menu = new ContextMenuViewModel(Dispatcher);

        if (IsConnected)
        {
            menu.AddItem("Disconnect", new RelayCommand(() => _twitchService.Disconnect()));
        }
        else
        {
            menu.AddItem("Connect To Twitch", new RelayCommand(async () => await _twitchService.ConnectAsync()));
        }

        return menu;
    }
}
