using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ObsWebSocket.Core;
using ObsWebSocket.Core.Events;
using ObsWebSocket.Core.Events.Generated;
using ObsWebSocket.Core.Protocol;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Generated;
using ObsWebSocket.Core.Protocol.Requests;
using ObsWebSocket.Core.Protocol.Responses;
using ObsWebSocket.Core.Serialization;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Obs;

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

public interface IObsController
{
    event EventHandler<SceneItemListReindexedPayload>? SceneItemUpdateRequested;
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    event EventHandler<CurrentProgramSceneChangedPayload>? CurrentProgramSceneChanged;
    event EventHandler<CurrentPreviewSceneChangedPayload>? CurrentPreviewSceneChanged;
    event EventHandler? SceneTransitionStarted;
    event EventHandler? StudioModeChanged;
    event EventHandler<SceneCreatedPayload>? SceneCreated;
    event EventHandler<SceneRemovedPayload>? SceneRemoved;
    
    bool IsConnectedToWebSocket { get; }
    bool StudioMode { get; }

    Task ConnectAsync();
    Task DisconnectAsync();

    Task SwitchStudioModeAsync();
    Task TransitionStudioModeAsync();

    Task<GetInputSettingsResponseData?> GetInputSettingsAsync(string sourceUuid);
    Task<GetVideoSettingsResponseData?> GetVideoSettingsAsync();
    Task<GetCurrentProgramSceneResponseData?> GetCurrentProgramSceneAsync();
    Task<GetSceneListResponseData?> GetSceneListAsync();
    
    Task CallBatchAsync(IEnumerable<BatchRequestItem> requests);

    Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input);
    Task SetCurrentPreviewSceneAsync(string scene);

    Task<List<SceneItemStub>> GetSceneItemListAsync(string? sceneName = null, string? sceneUuid = null);
    Task<List<SceneItemStub>> GetGroupSceneItemListAsync(string group);

    void SetStartedTransition(bool option);
}

public class ObsController : IObsController, IDisposable
{
    private readonly ITournamentState _tournamentState;
    private readonly IWebSocketMessageSerializer _webSocketMessageSerializer;
    public ILoggingService Logger { get; }

    private ObsWebSocketClientOptions ClientOptions { get; } = new();
    private ObsWebSocketClient Client { get; set; }
 
    public event EventHandler<SceneItemListReindexedPayload>? SceneItemUpdateRequested;
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<CurrentProgramSceneChangedPayload>? CurrentProgramSceneChanged;
    public event EventHandler<CurrentPreviewSceneChangedPayload>? CurrentPreviewSceneChanged;
    public event EventHandler? SceneTransitionStarted;
    public event EventHandler? StudioModeChanged;
    public event EventHandler<SceneCreatedPayload>? SceneCreated;
    public event EventHandler<SceneRemovedPayload>? SceneRemoved;

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
    
    public async Task SwitchStudioModeAsync()
    {
        if (!IsConnectedToWebSocket) return;
        
        try
        {
            await Client.SetStudioModeEnabledAsync(new SetStudioModeEnabledRequestData(!StudioMode));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    public async Task TransitionStudioModeAsync()
    {
        if (!IsConnectedToWebSocket) return;
        
        try
        {
            await Client.TriggerStudioModeTransitionAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
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
        
        Client.SceneCreated += OnSceneCreated;
        Client.SceneRemoved += OnSceneRemoved;
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
        
        Client.SceneCreated -= OnSceneCreated;
        Client.SceneRemoved -= OnSceneRemoved;
    }

    public async Task ConnectAsync()
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
            await DisconnectAsync();
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
            await DisconnectAsync();
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
            await DisconnectAsync();
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
            await DisconnectAsync();
        }
    }

    private void ChangeConnectionState(ConnectionState newState)
    {
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(State, newState));
        State = newState;
        _tryingToConnect = false;
    }
    
    public async Task DisconnectAsync() => await Client.DisconnectAsync();

    private void OnSceneCreated(object? sender, SceneCreatedEventArgs e) 
        => SceneCreated?.Invoke(this, new SceneCreatedPayload(e.EventData.IsGroup, e.EventData.SceneName, e.EventData.SceneUuid));
    private void OnSceneRemoved(object? sender, SceneRemovedEventArgs e) 
        => SceneRemoved?.Invoke(this, new SceneRemovedPayload(e.EventData.IsGroup, e.EventData.SceneName, e.EventData.SceneUuid));

    private void OnSceneItemListReindexed(object? sender, SceneItemListReindexedEventArgs e)
    {
        // nie pamietam sensu tego, ale wydaje mi sie za kompletnie zbedny event w mojej sytuacji
        //TODO: 7 jezeli przeniose item w scenie to nie resetuje povy graczy z racji tej ich kropki zeby nie duplikowac ich po povach
        // Task.Run(async ()=> { await UpdateSceneItems(e.SceneName); });

        SceneItemUpdateRequested?.Invoke(this, e.EventData);
    }
    private async void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs e)
    {
        try
        {
            GetSceneListResponseData? listResponse = await GetSceneListAsync();
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
            GetSceneListResponseData? listResponse = await GetSceneListAsync();
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
            GetCurrentProgramSceneResponseData? programScene = await GetCurrentProgramSceneAsync();
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

    public async Task<GetInputSettingsResponseData?> GetInputSettingsAsync(string sourceUuid) 
        => await Client.GetInputSettingsAsync(new GetInputSettingsRequestData(null, sourceUuid));
    public async Task<GetVideoSettingsResponseData?> GetVideoSettingsAsync() 
        => await Client.GetVideoSettingsAsync();
    public async Task<GetCurrentProgramSceneResponseData?> GetCurrentProgramSceneAsync() 
        => await Client.GetCurrentProgramSceneAsync();
    public async Task<GetSceneListResponseData?> GetSceneListAsync() 
        => await Client.GetSceneListAsync();
    
    public async Task CallBatchAsync(IEnumerable<BatchRequestItem> requests) 
        => await Client.CallBatchAsync(requests);

    public async Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input)
    {
        if (string.IsNullOrWhiteSpace(sourceUuid)) return;
        
        try
        {
            JsonElement element = JsonSerializer.SerializeToElement(input);
            await Client.SetInputSettingsAsync(new SetInputSettingsRequestData(element, null, sourceUuid));
        } 
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    public async Task SetCurrentPreviewSceneAsync(string scene)
    {
        if (!StudioMode) return;
        if (string.IsNullOrEmpty(scene)) return;
        
        try
        {
            await Client.SetCurrentPreviewSceneAsync(new SetCurrentPreviewSceneRequestData(scene));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    public async Task<List<SceneItemStub>> GetSceneItemListAsync(string? sceneName = null, string? sceneUuid = null)
    {
        GetSceneItemListResponseData? response = await Client.GetSceneItemListAsync(new GetSceneItemListRequestData(sceneName, sceneUuid));
        if (response == null) return [];
        return response.SceneItems ?? [];
    }
    public async Task<List<SceneItemStub>> GetGroupSceneItemListAsync(string group)
    {
        GetGroupSceneItemListResponseData? response = await Client.GetGroupSceneItemListAsync(new GetGroupSceneItemListRequestData(group));
        if (response == null) return [];
        return response.SceneItems ?? [];
    }

    public void SetStartedTransition(bool option)
    {
        _startedTransition = option;
    }
}
