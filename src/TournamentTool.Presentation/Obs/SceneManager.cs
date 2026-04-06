using System.Collections.ObjectModel;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Presentation.Obs;

public interface ISceneManager
{
    event EventHandler? ObsConnected;
    event EventHandler? ObsDisconnected;
    event EventHandler<string>? SelectedSceneUpdated; 
    
    Scene MainScene { get; }
    Scene PreviewScene { get; }
    
    ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    
    Task RefreshScenesPOVS();
    Task RefreshScenes();

    Task LoadPreviewScene(string sceneName, bool isFromApi = false);
    
    Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input);
    
    Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid);
    Task<List<(SceneItemStub, SceneItemStub?)>> GetSceneItemsAsync(string sceneName, string sceneUuid);

    void RegisterBinding(SceneItem sceneItem);
    Task PublishAsync(BindingKey key, object value);

    IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type);
    string GetHeadURL(string id, int size);
}

public class SceneManager : ISceneManager, IDisposable
{
    private readonly Settings _settings;
    
    private readonly IObsController _obs;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IBindingEngine _bindingEngine;
    private readonly ILoggingService _logger;

    public Scene MainScene { get; }
    public Scene PreviewScene { get; }

    private readonly ObservableCollection<SceneDto> _scenes = [];
    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    
    public event EventHandler? ObsConnected;
    public event EventHandler? ObsDisconnected;

    public event EventHandler<string>? SelectedSceneUpdated; 
    
    public bool BusyWithOBS { get; private set; }
    
    //TODO: 0 Pamietaj na rozbicie logiki domena -> viewmodel, czyli domena to logika ogolna, dzialania komunikacji w tle, dla aktualizowania
    // danych dla wszystkich scen bez potrzeby odpalonego SceneControllerViewModel jak wczesniej, a viewmodel to typowo logika tylko wymagana dla interakcji UI
    
    public SceneManager(IObsController obs, ITournamentPlayerRepository playerRepository, IBindingEngine bindingEngine, ILoggingService logger,
        ISettingsProvider settingsProvider)
    {
        _obs = obs;
        _playerRepository = playerRepository;
        _bindingEngine = bindingEngine;
        _logger = logger;

        Scenes = new ReadOnlyObservableCollection<SceneDto>(_scenes);
        
        _settings = settingsProvider.Get<Settings>();
        AppCache appCache = settingsProvider.Get<AppCache>();
        
        MainScene = new Scene(this, _logger, appCache, SceneType.Main);
        PreviewScene = new Scene(this, _logger, appCache, SceneType.Preview);

        _obs.SceneItemUpdateRequested += OnSceneUpdateRequested;
        _obs.ConnectionStateChanged += OnConnectionStateChanged;
        _obs.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
        _obs.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
        _obs.SceneTransitionStarted += OnSceneTransitionStarted;
        _obs.StudioModeChanged += OnStudioModeChanged;
        
        _obs.SceneCreated += OnSceneCreated;
        _obs.SceneRemoved += OnSceneRemoved;
    }

    private void OnSceneCreated(object? sender, SceneCreatedPayload e)
    {
        _scenes.Add(new SceneDto(e.SceneName ?? string.Empty, e.SceneUuid ?? string.Empty));
    }
    private void OnSceneRemoved(object? sender, SceneRemovedPayload e)
    {
        foreach (SceneDto scene in _scenes)
        {
            if (!scene.Uuid.Equals(e.SceneUuid)) continue;
            
            _scenes.Remove(scene);
            return;
        }
    }

    public void Dispose()
    {
        _obs.SceneItemUpdateRequested -= OnSceneUpdateRequested;
        _obs.ConnectionStateChanged -= OnConnectionStateChanged;
        _obs.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        _obs.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        _obs.SceneTransitionStarted -= OnSceneTransitionStarted;
        _obs.StudioModeChanged -= OnStudioModeChanged;
    }
    
    private async Task InitializeAsync()
    {
        GetSceneListResponseData? sceneResponse = await _obs.GetSceneList();
        if (sceneResponse != null)
        {
            _scenes.Clear();
            foreach (SceneStub scene in sceneResponse.Scenes ?? [])
            {
                _scenes.Add(SceneDto.Create(scene.SceneName, scene.SceneUuid));
            }
        }

        GetVideoSettingsResponseData? settings = await _obs.GetVideoSettings();
        if (settings != null)
        {
            MainScene.SetBaseWidth((float)settings.BaseWidth);
            PreviewScene.SetBaseWidth((float)settings.BaseWidth);
        }

        GetCurrentProgramSceneResponseData? mainScene = await _obs.GetCurrentProgramScene();
        if (mainScene != null)
        {
            await MainScene.SetSceneItemsAsync(mainScene.SceneName ?? string.Empty, mainScene.SceneUuid ?? string.Empty, true);
        }
        
        await StudioModeChanged();
    }
    
    private async Task OnOBSConnectedAsync()
    {
        ObsConnected?.Invoke(this, EventArgs.Empty);
        await InitializeAsync();
    }
    private void OnOBSDisconnected()
    {
        ClearPlayersFromPovs();
        ObsDisconnected?.Invoke(this, EventArgs.Empty);
    }
    
    private async void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        try
        {
            switch (e.NewState)
            {
                case ConnectionState.Connected: await OnOBSConnectedAsync(); break;
                case ConnectionState.Disconnected: OnOBSDisconnected(); break;
                case ConnectionState.Connecting:
                case ConnectionState.Disconnecting: break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    private async void OnStudioModeChanged(object? sender, EventArgs e)
    {
        try
        {
            await StudioModeChanged();
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    
    private async void OnSceneUpdateRequested(object? sender, SceneItemListReindexedPayload sceneItemListReindexedPayload)
    {
        try
        {
            await UpdateSceneItems(sceneItemListReindexedPayload.SceneName ?? string.Empty, sceneItemListReindexedPayload.SceneUuid ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    private async void OnCurrentProgramSceneChanged(object? sender, CurrentProgramSceneChangedPayload currentProgramSceneChangedPayload)
    {
        try
        {
            await CurrentMainSceneChanged(currentProgramSceneChangedPayload.SceneName ?? string.Empty, currentProgramSceneChangedPayload.SceneUuid ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    private async void OnCurrentPreviewSceneChanged(object? sender, CurrentPreviewSceneChangedPayload currentPreviewSceneChangedPayload)
    {
        try
        {
            await CurrentPreviewSceneChanged(currentPreviewSceneChangedPayload.SceneName ?? string.Empty, currentPreviewSceneChangedPayload.SceneUuid ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    private void OnSceneTransitionStarted(object? sender, EventArgs e)
    {
        if (MainScene.SceneName!.Equals(PreviewScene.SceneName)) return;
        
        _obs.SetStartedTransition(true);
        _logger.Log("Started Transition");
        
        MainScene.Swap(PreviewScene);
        SelectedSceneUpdated?.Invoke(this, PreviewScene.SceneName);
    }
    
    private async Task StudioModeChanged()
    {
        bool option = _obs.StudioMode;
        if (!option) return;
        
        PreviewScene.Clear();
        await LoadPreviewScene(MainScene.SceneName);
        // await LoadScenesForStudioMode();
    }
    
    private async Task CurrentMainSceneChanged(string sceneName, string sceneUuid)
    {
        bool isDuplicate = sceneUuid.Equals(MainScene.SceneUuid);
        _logger.Log($"Program scene: {sceneName}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

        //TODO: 0 To jest powod dlaczego nie aktulizuje sie zmieniona scena z obsa
        await MainScene.SetSceneItemsAsync(sceneName, sceneUuid);
    }
    private async Task CurrentPreviewSceneChanged(string sceneName, string sceneUuid)
    {
        if (sceneName.Equals(PreviewScene.SceneName)) return;
        if (sceneName.Equals(MainScene.SceneName))
        {
            PreviewScene.Clear();
            return;
        }
        _logger.Log("Loading Preview scene: " + sceneName);
        
        await PreviewScene.SetSceneItemsAsync(sceneName, sceneUuid);
        await LoadPreviewScene(sceneName, true);
    }
    
    private async Task UpdateSceneItems(string sceneName, string sceneUuid)
    {
        if (sceneName.Equals(MainScene.SceneName))
        {
            await MainScene.SetSceneItemsAsync(sceneName, sceneUuid, true);
            return;
        }

        await PreviewScene.SetSceneItemsAsync(sceneName, sceneUuid, true);
    }
    
    public async Task LoadPreviewScene(string sceneName, bool isFromApi = false)
    {
        if(!_obs.IsConnectedToWebSocket || string.IsNullOrEmpty(sceneName)) return;
        _logger.Log($"loading preview - {sceneName}");

        //TODO: 0 tu trzeba zrobic ladowanie sceny w obsie z poziomu tt
        /*UpdateSelectedScene(sceneName);

        if (isFromApi || SelectedScene.Equals(PreviewScene.SceneName)) return;
        await _obs.SetCurrentPreviewScene(SelectedScene);*/
    }
    
    public async Task RefreshScenesPOVS()
    {
        if (BusyWithOBS) return;
        BusyWithOBS = true;
        
        await MainScene.RefreshItems();
        await PreviewScene.RefreshItems();
        BusyWithOBS = false;
    }
    public async Task RefreshScenes()
    {
        if (!_obs.IsConnectedToWebSocket || BusyWithOBS) return;
        BusyWithOBS = true;
        
        ClearPlayersFromPovs();
        
        await MainScene.Refresh();
        await PreviewScene.Refresh();
        BusyWithOBS = false;
    }

    public async Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input)
        => await _obs.SetItemInputSettingsAsync(sourceUuid, input);
    public async Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid)
        => await _obs.GetInputSettingsAsync(sourceUuid);
    
    public void RegisterBinding(SceneItem sceneItem) 
        => _bindingEngine.RegisterTarget(sceneItem.BindingKey, sceneItem);
    public async Task PublishAsync(BindingKey key, object value) 
        => await _bindingEngine.PublishAsync(key, value);
    
    public IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type) 
        => _playerRepository.GetPlayerByStreamName(name, type);
    
    public void ClearPlayersFromPovs()
    {
        foreach (var player in _playerRepository.Players)
        {
            player.ClearPOVDependencies();
        }
    }
    
    public async Task<List<(SceneItemStub, SceneItemStub?)>> GetSceneItemsAsync(string sceneName, string sceneUuid)
    {
        List<(SceneItemStub, SceneItemStub?)> items = [];

        if (string.IsNullOrEmpty(sceneUuid)) return items;
        
        try
        {
            List<SceneItemStub> sceneItems = await _obs.GetSceneItemList(sceneName, sceneUuid);

            foreach (SceneItemStub item in sceneItems)
            {
                if (item.ExtensionData == null) continue;
                
                string itemInputKind = item.ExtensionData![nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
                
                string sourceType = item.ExtensionData[nameof(ExtensionDataType.sourceType)].ToString() ?? string.Empty;
                if (sourceType.Equals(nameof(SourceType.OBS_SOURCE_TYPE_SCENE)))
                {
                    List<SceneItemStub> groupItems = item.IsGroup == true ? await _obs.GetGroupSceneItemList(item.SourceName!) : [];
                    foreach (SceneItemStub groupItem in groupItems)
                    {
                        if (groupItem.ExtensionData == null) continue;
                        
                        string groupItemKind = groupItem.ExtensionData![nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(groupItemKind)) continue;

                        items.Add((groupItem, item));
                    }
                }
                
                if (string.IsNullOrEmpty(itemInputKind)) continue;

                items.Add((item, null));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }

        return items;
    }
    
    public string GetHeadURL(string id, int size) 
        => _settings.HeadAPIType.GetHeadURL(id, size);
}