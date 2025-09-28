using OBSStudioClient;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.ComponentModel;
using OBSStudioClient.Classes;
using OBSStudioClient.Responses;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Modules.Logging;
using TournamentTool.Utils.Parsers;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.OBS;

public enum ConnectionState
{
    Disconnected,
    Connected,
    Connecting,
    Disconnecting,
}
public class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionState OldState { get; }
    public ConnectionState NewState { get; }

    public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public class ObsController
{
    public TournamentViewModel Tournament { get; }
    private ISettings SettingsService { get; }
    public ILoggingService Logger { get; }

    public ObsClient Client { get; private set; }
 
    public event EventHandler<SceneNameEventArgs>? SceneItemUpdateRequested;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<SceneNameEventArgs>? CurrentProgramSceneChanged;
    public event EventHandler<SceneNameEventArgs>? CurrentPreviewSceneChanged;
    public event EventHandler? SceneTransitionStarted;
    public event EventHandler? StudioModeChanged;

    public bool IsConnectedToWebSocket { get; private set; }
    public bool StudioMode { get; private set; }
    public ConnectionState State { get; private set; }

    private bool _startedTransition;
    private bool _tryingToConnect;
    

    public ObsController(TournamentViewModel tournament, ISettings settingsService, ILoggingService logger)
    {
        Tournament = tournament;
        SettingsService = settingsService;
        Logger = logger;

        Client = new ObsClient { RequestTimeout = 10000 };
        Task.Run(Connect);
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

    public async Task Connect()
    {
        if (_tryingToConnect) return;
        _tryingToConnect = true;
        
        Client.PropertyChanged += OnPropertyChanged;
        const EventSubscriptions subscription = EventSubscriptions.All;
        await Client.ConnectAsync(true, SettingsService.Settings.Password!, "localhost", SettingsService.Settings.Port, subscription);
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
            ChangeStudioMode(studioMode);

            Client.StudioModeStateChanged += OnStudioModeStateChanged;
            Client.SceneItemListReindexed += OnSceneItemListReindexed;
            Client.SceneItemCreated += OnSceneItemCreated;
            Client.SceneItemRemoved += OnSceneItemRemoved;
            Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            Client.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
            Client.SceneTransitionStarted += OnSceneTransitionStarted;
            
            IsConnectedToWebSocket = true;
            State = ConnectionState.Connected;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Connected));
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            await Disconnect();
        }
    }
    public async Task Disconnect()
    {
        Client.PropertyChanged -= OnPropertyChanged;

        Client.StudioModeStateChanged -= OnStudioModeStateChanged;
        Client.SceneItemListReindexed -= OnSceneItemListReindexed;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        Client.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        Client.SceneTransitionStarted -= OnSceneTransitionStarted;

        Client.Disconnect();

        while (Client.ConnectionState != OBSStudioClient.Enums.ConnectionState.Disconnected)
        {
            await Task.Delay(100);
        }
        
        Client.Dispose();
        Client = new ObsClient { RequestTimeout = 10000 };
        IsConnectedToWebSocket = false;

        StudioMode = false;
        State = ConnectionState.Disconnected;
        _tryingToConnect = false;
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(State, ConnectionState.Disconnected));
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

    public async Task<(string?, int, StreamType)> GetBrowserURLStreamInfo(string sceneItemName)
    {
        if (!IsConnectedToWebSocket) return (string.Empty, 0, StreamType.twitch);

        var setting = await Client.GetInputSettings(sceneItemName);
        Dictionary<string, object> input = setting.InputSettings;

        input.TryGetValue("url", out var address);
        if (address == null)return (string.Empty, 0, StreamType.twitch); 

        string url = address!.ToString()!;
        if (string.IsNullOrEmpty(url))return (string.Empty, 0, StreamType.twitch); 

        try
        {
            return StreamUrlParser.Parse(url);
        } 
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        
        return (string.Empty, 0, StreamType.twitch);
    }

    private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
    {
        if (Client.ConnectionState == OBSStudioClient.Enums.ConnectionState.Disconnected) return;

        try
        {
            var existingItem = await Client.GetSceneItemId(sceneName, newSceneItemName);
            if (existingItem > 0)
            {
                Logger.Warning($"Scene item '{newSceneItemName}' already exists in scene '{sceneName}'.");
                return;
            }
        }
        catch { }

        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }
    public async Task CreateNestedSceneItem(string sceneName)
    {
        if (Client.ConnectionState == OBSStudioClient.Enums.ConnectionState.Disconnected) return;

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
                bool isConnected = Client!.ConnectionState == OBSStudioClient.Enums.ConnectionState.Connected;
                if (Client.ConnectionState is OBSStudioClient.Enums.ConnectionState.Connecting)
                {
                    const ConnectionState state = ConnectionState.Connecting;
                    ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(State, state));
                    State = state;
                }
                if (isConnected == IsConnectedToWebSocket) return;
                
                if (isConnected)
                {
                    await OnConnected();
                }
                else
                {
                    await Disconnect();
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
        }
    }
    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        //TODO: 7 jezeli przeniose item w scenie to nie resetuje povy graczy z racji tej ich kropki zeby nie duplikowac ich po povach
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
    }
    private async void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs e)
    {
        //TODO: 7 Zrobic wylapywanie dodawania wszystkich elementow tez typu head i text od povow
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        return;
        try
        {
            var listResponse = await Client.GetSceneList();
            var scenes = listResponse.Scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (scene.SceneName.Equals(e.SceneName)) return;
            }

            SceneItem[] items = await Client.GetSceneItemList(e.SceneName);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.SourceName.Equals(e.SourceName)) return;
            }
            
            Logger.Log(e.SceneName + " - " + e.SourceName);
            SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
        }
        catch
        {
            // ignored
        }
    }
    private async void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs e)
    {
        //TODO: 7 Zrobic wylapywanie usuwania wszystkich elementow tez typu head i text od povow
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        return;
        try
        {
            var listResponse = await Client.GetSceneList();
            var scenes = listResponse.Scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (scene.SceneName.Equals(e.SceneName)) return;
            }

            /*
            SceneItem[] items = await Client.GetSceneItemList(e.SceneName);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.SourceName.Equals(e.SourceName)) return;
            }
            */
            
            Logger.Log(e.SceneName + " - " + e.SourceName);
            SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName));
        }
        catch
        {
            // ignored
        }
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
    private void ChangeStudioMode(bool option)
    {
        Logger.Log($"StudioMode: {option}");
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
