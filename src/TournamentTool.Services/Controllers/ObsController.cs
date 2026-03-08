using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ObsWebSocket.Core;
using ObsWebSocket.Core.Events;
using ObsWebSocket.Core.Events.Generated;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Generated;
using ObsWebSocket.Core.Protocol.Requests;
using ObsWebSocket.Core.Protocol.Responses;
using ObsWebSocket.Core.Serialization;
using TournamentTool.Core.Parsers;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Controllers;

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

public interface IObsItemController
{
    //TODO: 0 rozbic odpowiedzialnosc tak zeby byl oddzielny serwis dla z obsem pod zarzadzanie itemami, wteyd scena moze posiadac 
    // jego abstracke i to spowoduje, ze bedzie czytelniejsze zastosowanie tego
}

public interface IObsController
{
    event EventHandler<SceneItemListReindexedPayload>? SceneItemUpdateRequested;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<CurrentProgramSceneChangedPayload>? CurrentProgramSceneChanged;
    event EventHandler<CurrentPreviewSceneChangedPayload>? CurrentPreviewSceneChanged;
    event EventHandler? SceneTransitionStarted;
    event EventHandler? StudioModeChanged;
    
    bool IsConnectedToWebSocket { get; }
    bool StudioMode { get; }
}

public class ObsController : IObsController, IDisposable
{
    private readonly ITournamentState _tournamentState;
    private readonly IWebSocketMessageSerializer _webSocketMessageSerializer;
    public ILoggingService Logger { get; }

    public ObsWebSocketClientOptions ClientOptions { get; } = new();
    public ObsWebSocketClient Client { get; private set; }
 
    public event EventHandler<SceneItemListReindexedPayload>? SceneItemUpdateRequested;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<CurrentProgramSceneChangedPayload>? CurrentProgramSceneChanged;
    public event EventHandler<CurrentPreviewSceneChangedPayload>? CurrentPreviewSceneChanged;
    public event EventHandler? SceneTransitionStarted;
    public event EventHandler? StudioModeChanged;

    public bool IsConnectedToWebSocket { get; private set; }
    public bool StudioMode { get; private set; }

    public ConnectionState State { get; private set; }
    
    private readonly Settings _settings;

    private bool _startedTransition;
    private bool _tryingToConnect;
    

    public ObsController(ITournamentState tournamentState, ISettingsProvider settingsProvider, ILoggingService logger, 
        IWebSocketMessageSerializer webSocketMessageSerializer)
    {
        _tournamentState = tournamentState;
        _webSocketMessageSerializer = webSocketMessageSerializer;
        Logger = logger;
        
        _settings = settingsProvider.Get<Settings>();

        _tournamentState.PresetChanged += PresetChanged;
        Client = new ObsWebSocketClient(NullLogger<ObsWebSocketClient>.Instance, _webSocketMessageSerializer, Options.Create(ClientOptions));
        CreateClient();
    }
    public void Dispose()
    {
        _tournamentState.PresetChanged -= PresetChanged;
        
        ClearClient();
    }

    public async void PresetChanged(object? sender, Tournament? tournament)
    {
        if (tournament == null || !IsConnectedToWebSocket) return;
        if (string.IsNullOrEmpty(tournament.SceneCollection)) return;

        try
        {
            await Client.SetCurrentSceneCollectionAsync(new SetCurrentSceneCollectionRequestData(tournament.SceneCollection));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    
    public void SwitchStudioMode()
    {
        if (!IsConnectedToWebSocket) return;
        Client.SetStudioModeEnabledAsync(new SetStudioModeEnabledRequestData(!StudioMode));
    }
    public async Task TransitionStudioModeAsync()
    {
        if (!IsConnectedToWebSocket) return;
        await Client.TriggerStudioModeTransitionAsync();
    }

    private void CreateClient()
    {
        Client = new ObsWebSocketClient(NullLogger<ObsWebSocketClient>.Instance, _webSocketMessageSerializer, Options.Create(ClientOptions));
        
        Client.Connected += OnConnected;
        Client.Connecting += OnConnecting;
        Client.ConnectionFailed += OnConnectionFailed;
        Client.Disconnected += OnDisconnected;
        
        Client.StudioModeStateChanged += OnStudioModeStateChanged;
        Client.SceneItemListReindexed += OnSceneItemListReindexed;
        Client.CurrentSceneCollectionChanged += OnSceneCollectionChanged;
        Client.SceneItemCreated += OnSceneItemCreated;
        Client.SceneItemRemoved += OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
        Client.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
        Client.SceneTransitionStarted += OnSceneTransitionStarted;
    }
    private void ClearClient()
    {
        Client.Connected -= OnConnected;
        Client.Connecting -= OnConnecting;
        Client.ConnectionFailed -= OnConnectionFailed;
        Client.Disconnected -= OnDisconnected;
        
        Client.StudioModeStateChanged -= OnStudioModeStateChanged;
        Client.SceneItemListReindexed -= OnSceneItemListReindexed;
        Client.CurrentSceneCollectionChanged -= OnSceneCollectionChanged;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        Client.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        Client.SceneTransitionStarted -= OnSceneTransitionStarted;
    }

    public async Task Connect()
    {
        if (_tryingToConnect) return;
        _tryingToConnect = true;

        ClientOptions.EventSubscriptions = (uint)EventSubscription.All;
        ClientOptions.ServerUri = new Uri($"ws://localhost:{_settings.Port}/");
        ClientOptions.Password = _settings.Password;
        
        await Client.ConnectAsync();
    }
    private async void OnConnected(object? sender, EventArgs eventArgs)
    {
        try
        {
            if (IsConnectedToWebSocket) return;
            
            GetStudioModeEnabledResponseData? studioMode = await Client.GetStudioModeEnabledAsync();
            if (studioMode != null)
            {
                ChangeStudioMode(studioMode.StudioModeEnabled);
            }
            
            IsConnectedToWebSocket = true;
            ChangeConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            await Disconnect();
        }
    }
    private async void OnConnecting(object? sender, ConnectingEventArgs e)
    {
        try
        {
            if (State == ConnectionState.Connected) return;
            ChangeConnectionState(ConnectionState.Connecting);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            await Disconnect();
        }
    }
    private async void OnDisconnected(object? sender, DisconnectedEventArgs e)
    {
        try
        {
            IsConnectedToWebSocket = false;
            ChangeConnectionState(ConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            await Disconnect();
        }
    }
    
    private async void OnConnectionFailed(object? sender, ConnectionFailedEventArgs e)
    {
        try
        {
            //TODO: 0 Obczaic sytuacje w ktorych sie to odpala
            // ChangeConnectionState(ConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            await Disconnect();
        }
    }

    private void ChangeConnectionState(ConnectionState newState)
    {
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(State, newState));
        State = newState;
        _tryingToConnect = false;
    }
    
    public async Task Disconnect() => await Client.DisconnectAsync();

    public async Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input)
    {
        if (string.IsNullOrWhiteSpace(sourceUuid)) return;
        
        JsonElement element = JsonSerializer.SerializeToElement(input);
        await Client.SetInputSettingsAsync(new SetInputSettingsRequestData(element, null, sourceUuid));
    }
    
    public async Task<(string?, int, StreamType)> GetBrowserURLStreamInfo(string sourceUuid)
    {
        if (!IsConnectedToWebSocket) return (string.Empty, 0, StreamType.twitch);

        GetInputSettingsResponseData? settingsResponse = await Client.GetInputSettingsAsync(new GetInputSettingsRequestData(null, sourceUuid));
        if (settingsResponse == null ||
            !settingsResponse.InputSettings.HasValue ||
            !settingsResponse.InputSettings.Value.TryGetProperty("url", out JsonElement urlElement)) 
            return (string.Empty, 0, StreamType.twitch);

        string url = urlElement.ToString();
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

    /*private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
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
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }*/
    /*public async Task CreateNestedSceneItem(string sceneName)
    {
        if (Client.ConnectionState == OBSStudioClient.Enums.ConnectionState.Disconnected) return;

        await Client.CreateScene(sceneName);

        await CreateNewSceneItem(sceneName, "item1", "browser_source");
        await CreateNewSceneItem(sceneName, "item2", "browser_source");

        int sceneItem1 = await Client.GetSceneItemId(sceneName, "item1");
        int sceneItem2 = await Client.GetSceneItemId(sceneName, "item2");

        //Client.setscene
        //await Client.CreateSceneItem(CurrentSceneName, sceneName);
    }*/

    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        //TODO: 7 jezeli przeniose item w scenie to nie resetuje povy graczy z racji tej ich kropki zeby nie duplikowac ich po povach
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        SceneItemUpdateRequested?.Invoke(this, e.EventData);
    }
    private async void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs e)
    {
        try
        {
            GetSceneListResponseData? listResponse = await GetSceneList();
            if (listResponse == null) return;

            Logger.Log(e.EventData.SceneName + " - " + e.EventData.SourceName);
            //TODO: 1 Prawdziwy update itemu do stworzenia
            // SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.SceneName, e.SceneUuid));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private async void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs e)
    {
        try
        {
            GetSceneListResponseData? listResponse = await GetSceneList();
            if (listResponse == null) return;

            Logger.Log(e.EventData.SceneName + " - " + e.EventData.SourceName);
            //TODO: 1 Prawdziwy update itemu do usuniecia
            // SceneItemUpdateRequested?.Invoke(this, new SceneNameEventArgs(e.EventData.SceneName, e.EventData.SceneUuid));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private async void OnSceneCollectionChanged(object? sender, CurrentSceneCollectionChangedEventArgs currentSceneCollectionChangedEventArgs)
    {
        try
        {
            GetCurrentProgramSceneResponseData? programScene = await GetCurrentProgramScene();
            if (programScene == null) return;
            
            CurrentProgramSceneChanged?.Invoke(this, new CurrentProgramSceneChangedPayload(programScene.SceneName, programScene.SceneUuid));
            CurrentPreviewSceneChanged?.Invoke(this, new CurrentPreviewSceneChangedPayload(" ", " "));
            //tu jest bardzo prosty trik z poprawnym wczytywaniem studio mode zeby w kodzie dalej lapac przy zmianie scene collection wczytywanie scen pod preview
            //jest to tymczasowe rozwiazanie z racji reworku komunikacji z obsem
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private void OnCurrentProgramSceneChanged(object? sender, CurrentProgramSceneChangedEventArgs currentProgramSceneChangedEventArgs)
    {
        if (StudioMode) return;
        CurrentProgramSceneChanged?.Invoke(this, currentProgramSceneChangedEventArgs.EventData);
    }
    private void OnCurrentPreviewSceneChanged(object? sender, CurrentPreviewSceneChangedEventArgs currentPreviewSceneChangedEventArgs)
    {
        if (_startedTransition)
        {
            _startedTransition = false;
            return;
        }
        CurrentPreviewSceneChanged?.Invoke(this, currentPreviewSceneChangedEventArgs.EventData);
    }

    private void OnStudioModeStateChanged(object? sender, StudioModeStateChangedEventArgs studioModeStateChangedEventArgs)
    {
        ChangeStudioMode(studioModeStateChangedEventArgs.EventData.StudioModeEnabled);
    }
    private void ChangeStudioMode(bool option)
    {
        Logger.Log($"StudioMode: {option}");
        StudioMode = option;
        
        StudioModeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSceneTransitionStarted(object? sender, SceneTransitionStartedEventArgs sceneTransitionStartedEventArgs)
    {
        if (!StudioMode) return;
        SceneTransitionStarted?.Invoke(this, EventArgs.Empty);
    }

    public async Task<GetVideoSettingsResponseData?> GetVideoSettings()
    {
        return await Client.GetVideoSettingsAsync();
    }
    public async Task<GetCurrentProgramSceneResponseData?> GetCurrentProgramScene()
    {
        return await Client.GetCurrentProgramSceneAsync();
    }
    public async Task<List<SceneItemStub>> GetSceneItemList(string? sceneName = null, string? sceneUuid = null)
    {
        GetSceneItemListResponseData? response = await Client.GetSceneItemListAsync(new GetSceneItemListRequestData(sceneName, sceneUuid));
        if (response == null) return [];
        return response.SceneItems ?? [];
    }
    public async Task<List<SceneItemStub>> GetGroupSceneItemList(string group)
    {
        GetGroupSceneItemListResponseData? response = await Client.GetGroupSceneItemListAsync(new GetGroupSceneItemListRequestData(group));
        if (response == null) return [];
        return response.SceneItems ?? [];
    }
    public async Task<GetSceneListResponseData?> GetSceneList()
    {
        return await Client.GetSceneListAsync();
    }

    public async Task SetCurrentPreviewScene(string scene)
    {
        await Client.SetCurrentPreviewSceneAsync(new SetCurrentPreviewSceneRequestData(scene));
    }

    public void SetStartedTransition(bool option)
    {
        _startedTransition = option;
    }
}
