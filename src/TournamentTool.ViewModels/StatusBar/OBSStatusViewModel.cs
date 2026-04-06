using TournamentTool.Core.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public class OBSStatusViewModel : StatusItemViewModel
{
    private readonly IObsController _obsController;
    private readonly ISceneManager _sceneManager;

    protected override string Name => "OBS";
     
    private ConnectionState _currentState;
 
    
    public OBSStatusViewModel(IObsController obsController, ISceneManager sceneManager, IDispatcherService dispatcher, IImageService imageService, 
        IMenuService menuService) : base(dispatcher, imageService, menuService)
    {
        _obsController = obsController;
        _sceneManager = sceneManager;
        
        _obsController.ConnectionStateChanged += OnConnectionStateChanged;
    }
    public override void Dispose()
    {
        _obsController.ConnectionStateChanged -= OnConnectionStateChanged;
    }
 
    protected override void InitializeImages()
    {
        AddStateImage("off", "StatusBarIcons/obs-off.png");
        AddStateImage("wait", "StatusBarIcons/obs-wait.png");
        AddStateImage("on", "StatusBarIcons/obs-on.png");
    }
    protected override void InitializeState()
    {
        SetState("off");
        OnConnectionStateChanged(this, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Disconnected));
    }
    
    public override ContextMenuViewModel BuildContextMenu()
    {
        var menu = new ContextMenuViewModel(Dispatcher);

        switch (_currentState)
        {
            case ConnectionState.Disconnected:
                menu.AddItem("Connect to OBS", new RelayCommand(async () => await ConnectOBS()));
                break;
            case ConnectionState.Connecting:
                menu.AddItem("Stop connecting", new RelayCommand(async () => await DisconnectOBS()));
                break;
            case ConnectionState.Connected:
                menu.AddItem("Disconnect", new RelayCommand(async () => await DisconnectOBS()));
                break;
        }

        return menu;
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
         
        SetToolTip($"{_currentState}");
    }

    private async Task ConnectOBS()
    {
        if (_currentState == ConnectionState.Connected) return;
        await _obsController.Connect();    
    }
    private async Task DisconnectOBS()
    {
        if (_currentState == ConnectionState.Disconnected) return;
        await _obsController.Disconnect();
    }
}