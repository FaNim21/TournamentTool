using OBSStudioClient;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using OBSStudioClient.Classes;
using OBSStudioClient.Responses;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.OBS;

public enum OBSConnectionState
{
    Disconnected,
    Connected,
    Connecting,
    Disconnecting,
}
public class ConnectionStateChangedEventArgs : EventArgs
{
    public OBSConnectionState OldState { get; }
    public OBSConnectionState NewState { get; }

    public ConnectionStateChangedEventArgs(OBSConnectionState oldState, OBSConnectionState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public class ObsController
{
    public TournamentViewModel Tournament { get; }
    
    private CancellationTokenSource _cancellationTokenSource;

    public ObsClient Client { get; private set; }
 
    // takie rzeczy to do statusbarviewmodel
    // public Brush? IsConnectedColor { get; set; }

    public event EventHandler<SceneNameEventArgs>? SceneItemUpdateRequested;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<SceneNameEventArgs>? CurrentProgramSceneChanged;
    public event EventHandler<SceneNameEventArgs>? CurrentPreviewSceneChanged;
    public event EventHandler? SceneTransitionStarted;
    public event EventHandler? StudioModeChanged;

    public bool IsConnectedToWebSocket { get; private set; }
    public bool StudioMode { get; private set; }
 
    private bool _startedTransition;


    public ObsController(TournamentViewModel tournament)
    {
        Tournament = tournament;
        
        Client = new ObsClient { RequestTimeout = 10000 };

        _cancellationTokenSource = new CancellationTokenSource();
        Task.Factory.StartNew(async () =>
        {
            await Connect(Tournament.Password!, Tournament.Port);
        }, _cancellationTokenSource.Token);
        
        // AddPovItemToOBSCommand = new RelayCommand(async () => { await OBS.CreateNestedSceneItem("PovSceneLOL"); });
    }
 
    public void SwitchStudioMode()
    {
        if (!IsConnectedToWebSocket) return;
        Client.SetStudioModeEnabled(!StudioMode);
    }
    public void TransitionStudioMode()
    {
        if (!IsConnectedToWebSocket) return;
        Client.TriggerStudioModeTransition();
    }

    public async Task Connect(string password, int port)
    {
        const EventSubscriptions subscription = EventSubscriptions.All;
        await Client.ConnectAsync(true, password, "localhost", port, subscription);
        Client.PropertyChanged += OnPropertyChanged;
    }
    private async Task OnConnected()
    {
        if (IsConnectedToWebSocket) return;
        try
        {
            if (!string.IsNullOrEmpty(Tournament.SceneCollection))
            {
                await Client.SetCurrentSceneCollection(Tournament.SceneCollection);
            }

            bool studioMode = await Client.GetStudioModeEnabled();
            ChangeStudioMode(studioMode, false);

            Client.StudioModeStateChanged += OnStudioModeStateChanged;
            Client.SceneItemListReindexed += OnSceneItemListReindexed;
            Client.SceneItemCreated += OnSceneItemCreated;
            Client.SceneItemRemoved += OnSceneItemRemoved;
            Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            Client.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
            Client.SceneTransitionStarted += OnSceneTransitionStarted;
            
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(OBSConnectionState.Disconnected, OBSConnectionState.Connected));
            IsConnectedToWebSocket = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message} - {ex.StackTrace}");
            await Disconnect();
        }
    }
    public async Task Disconnect()
    {
        if (!IsConnectedToWebSocket) return;

        Client.PropertyChanged -= OnPropertyChanged;

        Client.StudioModeStateChanged -= OnStudioModeStateChanged;
        Client.SceneItemListReindexed -= OnSceneItemListReindexed;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        Client.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        Client.SceneTransitionStarted -= OnSceneTransitionStarted;

        await _cancellationTokenSource.CancelAsync();
        _cancellationTokenSource.Dispose();

        Client.Disconnect();

        while (Client.ConnectionState != ConnectionState.Disconnected)
        {
            await Task.Delay(100);
        }
        
        Client.Dispose();
        Client = new ObsClient();
        IsConnectedToWebSocket = false;
        
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(OBSConnectionState.Connected, OBSConnectionState.Disconnected));
    }

    public void SetBrowserURL(PointOfView pov)
    {
        if (pov == null) return;
        if (!SetBrowserURL(pov.SceneItemName!, pov.GetURL())) return;

        if (Tournament.SetPovHeadsInBrowser) pov.UpdateHead();
        if (Tournament.DisplayedNameType != DisplayedNameType.None) pov.UpdateNameTextField();
        if (Tournament.SetPovPBText) pov.UpdatePersonalBestTextField();
    }
    public bool SetBrowserURL(string sceneItemName, string path)
    {
        if (!IsConnectedToWebSocket || string.IsNullOrEmpty(sceneItemName)) return false;

        Dictionary<string, object> input = new() { { "url", path }, };
        Client.SetInputSettings(sceneItemName, input);
        return true;
    }
    public void SetTextField(string sceneItemName, string text)
    {
        if (!IsConnectedToWebSocket || string.IsNullOrEmpty(sceneItemName)) return;

        Dictionary<string, object> input = new() { { "text", text }, };
        Client.SetInputSettings(sceneItemName, input);
    }

    public async Task<(string, float)> GetBrowserURLTwitchName(string sceneItemName)
    {
        if (!IsConnectedToWebSocket) return (string.Empty, 0);

        var setting = await Client.GetInputSettings(sceneItemName);
        Dictionary<string, object> input = setting.InputSettings;

        string patternPlayerName = @"channel=([^&]+)";
        string patternVolume = @"volume=(\d+(\.\d+)?)";

        input.TryGetValue("url", out var address);
        if (address == null) return (string.Empty, 0);

        string url = address!.ToString()!;
        if (string.IsNullOrEmpty(url)) return (string.Empty, 0);

        string name = string.Empty;
        float volume = 0;

        Match matchName = Regex.Match(address!.ToString()!, patternPlayerName);
        Match matchVolume = Regex.Match(address!.ToString()!, patternVolume);

        if (matchName.Success)
        {
            name = matchName.Groups[1].Value;
        }
        if (matchVolume.Success)
        {
            if (float.TryParse(matchVolume.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float volumeValue))
            {
                volume = volumeValue;
            }
        }

        return (name, volume);
    }

    private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
    {
        if (Client.ConnectionState == ConnectionState.Disconnected) return;

        try
        {
            var existingItem = await Client.GetSceneItemId(sceneName, newSceneItemName);
            if (existingItem > 0)
            {
                DialogBox.Show($"Scene item '{newSceneItemName}' already exists in scene '{sceneName}'.");
                return;
            }
        }
        catch { }

        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }

    public async Task CreateNestedSceneItem(string sceneName)
    {
        if (Client.ConnectionState == ConnectionState.Disconnected) return;

        await Client.CreateScene(sceneName);

        await CreateNewSceneItem(sceneName, "item1", "browser_source");
        await CreateNewSceneItem(sceneName, "item2", "browser_source");

        int sceneItem1 = await Client.GetSceneItemId(sceneName, "item1");
        int sceneItem2 = await Client.GetSceneItemId(sceneName, "item2");

        //Client.setscene
        //await Client.CreateSceneItem(CurrentSceneName, sceneName);
    }

    private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == "ConnectionState")
            {
                bool isConnected = Client!.ConnectionState == ConnectionState.Connected;
                if (isConnected == IsConnectedToWebSocket) return;
                Trace.WriteLine($"Connection with obs changed to: {Client!.ConnectionState}");

                if (!isConnected)
                {
                    // IsConnectedColor = new SolidColorBrush(Consts.OfflineColor);
                    // TODO: 0
                    // Tu moze nie jest dobrym pomyslem robic full disconnect w formie robienia client.dispose
                    // zeby tez sprobowac lapac stala kontrole na zmianami w kwesti polaczenia
                    // wiec trzeba po prostu czyscic eventy zeby nie robic memeory leakow tylko z tym,
                    // a tak to trzeymac onproperty w kwestii zmian
                    await Disconnect();
                }
                else
                {
                    // IsConnectedColor = new SolidColorBrush(Consts.LiveColor);
                    await OnConnected();
                }
            }
            // OnPropertyChanged(nameof(IsConnectedColor));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        //TODO: 7 jezeli przeniose item w scenie to nie resetuje povy graczy z racji tej ich kropki zeby nie duplikowac ich po povach
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
    }
    private void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs e)
    {
        //TODO: 7 Zrobic wylapywanie dodawania wszystkich elementow tez typu head i text od povow
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });
        
        SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
        
        /*if (!args.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        SceneItemTransform transform = Client!.GetSceneItemTransform(args.SceneName, args.SceneItemId).Result;
        PointOfView pov = new(this)
        {
            SceneName = args.SceneName,
            SceneItemName = args.SourceName,
            ID = args.SceneItemId,
            X = (int)(transform.PositionX / XAxisRatio),
            Y = (int)(transform.PositionY / YAxisRatio),
            Width = (int)(transform.Width / XAxisRatio),
            Height = (int)(transform.Height / YAxisRatio),
            Text = args.SourceName
        };
        Controller.AddPov(pov);*/
    }
    private void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs e)
    {
        //TODO: 7 Zrobic wylapywanie usuwania wszystkich elementow tez typu head i text od povow
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
        
        /*if (!args.SourceName.StartsWith(Controller.Configuration.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        PointOfView? pov = null;
        for (int i = 0; i < Controller.POVs.Count; i++)
        {
            if (Controller.POVs[i].SceneItemName == args.SourceName)
            {
                pov = Controller.POVs[i];
                break;
            }
        }

        if (pov == null) return;
        Controller.RemovePov(pov);*/
    }

    private void OnCurrentProgramSceneChanged(object? sender, SceneNameEventArgs e)
    {
        if (StudioMode) return;
        CurrentProgramSceneChanged?.Invoke(this, e);
    }
    private void OnCurrentPreviewSceneChanged(object? sender, SceneNameEventArgs e)
    {
        if (_startedTransition)
        {
            _startedTransition = false;
            return;
        }
        CurrentPreviewSceneChanged?.Invoke(this, e);
    }

    private void OnStudioModeStateChanged(object? sender, StudioModeStateChangedEventArgs e)
    {
        ChangeStudioMode(e.StudioModeEnabled);
    }
    private void ChangeStudioMode(bool option, bool refresh = true)
    {
        Trace.WriteLine($"StudioMode: {option}");
        StudioMode = option;
        
        StudioModeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSceneTransitionStarted(object? sender, TransitionNameEventArgs e)
    {
        if (!StudioMode) return;
        SceneTransitionStarted?.Invoke(this, EventArgs.Empty);
    }

    public async Task<VideoSettingsResponse> GetVideoSettings()
    {
        return await Client.GetVideoSettings();
    }
    public async Task<string> GetCurrentProgramScene()
    {
        return await Client.GetCurrentProgramScene();
    }
    public async Task<SceneItem[]> GetSceneItemList(string scene)
    {
        return await Client.GetSceneItemList(scene);
    }
    public async Task<SceneItem[]> GetGroupSceneItemList(string group)
    {
        return await Client.GetGroupSceneItemList(group);
    }
    public async Task<SceneListResponse> GetSceneList()
    {
        return await Client.GetSceneList();
    }

    public async Task SetCurrentPreviewScene(string scene)
    {
        await Client.SetCurrentPreviewScene(scene);
    }

    public void SetStartedTransition(bool option)
    {
        _startedTransition = option;
    }
}
