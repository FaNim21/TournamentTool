using System.Windows;
using System.Windows.Controls;
using TournamentTool.Commands;
using TournamentTool.Modules.OBS;

namespace TournamentTool.ViewModels.StatusBar;

public class OBSStatusViewModel : StatusItemViewModel
{
    private readonly ObsController _obsController;

    protected override string Name => "OBS";
     
    private ConnectionState _currentState;
 
    
    public OBSStatusViewModel(ObsController obsController)
    {
        _obsController = obsController;
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
    protected override void BuildContextMenu(ContextMenu menu)
    {
        menu.Items.Clear();

        if (_currentState == ConnectionState.Disconnected)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Connect to OBS",
                Command = new RelayCommand(async () => await ConnectOBS())
            });
        }
        else if (_currentState is ConnectionState.Connecting)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Stop connecting",
                Command = new RelayCommand(async () => await DisconnectOBS())
            });
        }
        else if (_currentState is ConnectionState.Connected)
        {
            menu.Items.Add(new MenuItem 
            { 
                Header = "Disconnect",
                Command = new RelayCommand(async () => await DisconnectOBS())
            });
        }
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