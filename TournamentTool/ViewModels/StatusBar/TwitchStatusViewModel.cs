using System.Windows.Controls;
using TournamentTool.Commands;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.StatusBar;

public class TwitchStatusViewModel : StatusItemViewModel
{
    private readonly TwitchService _twitchService;
    private readonly TournamentViewModel _tournament;

    protected override string Name => "Twitch";
    
    private bool IsConnected => _twitchService.IsConnected;
    private ConnectionState _currentState;

    
    public TwitchStatusViewModel(TwitchService twitchService, TournamentViewModel tournament)
    {
        _twitchService = twitchService;
        _tournament = tournament;
        
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
        
        SetToolTip(e.NewState.ToString());
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
    
    protected override void BuildContextMenu(ContextMenu menu)
    {
        menu.Items.Clear();

        if (IsConnected)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Disconnect",
                Command = new RelayCommand(_twitchService.Disconnect)
            });
        }
        else
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Connect to Twitch",
                Command = new RelayCommand(async () => await _twitchService.ConnectAsync())
            });
        }
    }
}
