using System.Collections.ObjectModel;
using System.Text.Json;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Requests;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities.Obs;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Factories;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;

namespace TournamentTool.Presentation.Obs;

public sealed class SceneManager : ISceneManager, ISceneItemGetter, IDisposable
{
    private readonly IObsController _obs;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly ILoggingService _logger;
    private readonly IDispatcherService _dispatcher;
    private readonly IObsUpdateBatcher _obsUpdateBatcher;

    public Scene MainScene { get; }
    public Scene PreviewScene { get; }

    public List<Scene> AdditionalScenes { get; } = [];

    private readonly ObservableCollection<SceneDto> _scenes = [];
    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    
    public event EventHandler? ObsConnected;
    public event EventHandler? ObsDisconnected;

    public event EventHandler<string>? SelectedSceneUpdated; 
    
    public bool BusyWithOBS { get; private set; }


    public SceneManager(IObsController obs, ITournamentPlayerRepository playerRepository, ISceneFactory sceneFactory, ILoggingService logger,
        IDispatcherService dispatcher, IObsUpdateBatcher obsUpdateBatcher)
    {
        _obs = obs;
        _playerRepository = playerRepository;
        _logger = logger;
        _dispatcher = dispatcher;
        _obsUpdateBatcher = obsUpdateBatcher;

        Scenes = new ReadOnlyObservableCollection<SceneDto>(_scenes);
        
        MainScene = sceneFactory.Create(this, SceneType.Main);
        PreviewScene = sceneFactory.Create(this, SceneType.Preview);

        _obs.SceneItemUpdateRequested += OnSceneUpdateRequested;
        _obs.ConnectionStateChanged += OnConnectionStateChanged;
        _obs.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
        _obs.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
        _obs.SceneTransitionStarted += OnSceneTransitionStarted;
        _obs.StudioModeChanged += OnStudioModeChanged;
        
        _obs.SceneCreated += OnSceneCreated;
        _obs.SceneRemoved += OnSceneRemoved;
        _obs.SceneItemCreated += OnSceneItemCreated;
        _obs.SceneItemRemoved += OnSceneItemRemoved;
    }
    public void Dispose()
    {
        _obs.SceneItemUpdateRequested -= OnSceneUpdateRequested;
        _obs.ConnectionStateChanged -= OnConnectionStateChanged;
        _obs.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        _obs.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        _obs.SceneTransitionStarted -= OnSceneTransitionStarted;
        _obs.StudioModeChanged -= OnStudioModeChanged;
        
        _obs.SceneCreated -= OnSceneCreated;
        _obs.SceneRemoved -= OnSceneRemoved;
        _obs.SceneItemCreated -= OnSceneItemCreated;
        _obs.SceneItemRemoved -= OnSceneItemRemoved;
    }

    private void OnSceneCreated(object? sender, SceneCreatedPayload e)
    {
        SceneDto newScene = new(e.SceneName ?? string.Empty, e.SceneUuid ?? string.Empty);
        if (Scenes.Contains(newScene)) return;
        
        _dispatcher.Invoke(()=>
        {
            _scenes.Add(newScene);
        });
    }
    private void OnSceneRemoved(object? sender, SceneRemovedPayload e)
    {
        foreach (SceneDto scene in _scenes)
        {
            if (!scene.Uuid.Equals(e.SceneUuid)) continue;
            
            _dispatcher.Invoke(()=>
            {
                _scenes.Remove(scene);
            });
            return;
        }
    }
    
    private async void OnSceneItemCreated(object? sender, SceneItemCreatedPayload e)
    {
        //TODO: 0 Problem tutaj kuzwa jest taki, ze nie ma info o grupie co jest wazne i trzeba sprawdzic czy sceneuuid i name to
        // tak na prawde group uuid i name jezeli tworzymy scene item w grupie, a jak w scenie to wtedy dane od rzeczywistej sceny
        // z racji tego ze grupy to i tak sceny w obsie -.-
        
        try
        {
            List<SceneItemStub> sceneItems = await _obs.GetSceneItemListAsync(e.SceneName, e.SceneUuid);
            
            SceneItemStub? newSceneItem = sceneItems.FirstOrDefault(s => s.SourceUuid is { } && s.SourceUuid.Equals(e.SourceUuid));
            if (newSceneItem is null) return;

            if (MainScene.SceneUuid.Equals(e.SceneUuid))
            {
                await MainScene.AddSceneItemAsync(newSceneItem);
            }
            else if (PreviewScene.SceneUuid.Equals(e.SceneUuid))
            {
                await PreviewScene.AddSceneItemAsync(newSceneItem);
            }

            foreach (var additionalScene in AdditionalScenes)
            {
                if (!additionalScene.SceneUuid.Equals(e.SceneUuid)) continue;
                
                await additionalScene.AddSceneItemAsync(newSceneItem);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }
    private void OnSceneItemRemoved(object? sender, SceneItemRemovedPayload e)
    {
        //TODO: 0 NIE DZIALA TO W SCENE CONFIG Z RACJI DUPLIKATOW, a przydalaby sie logika zdarzen w scene configu, nawet to jest wazniejsze niz jak na zywo usuwac w controllerze,
        // bo w samym controllerze to da tylko wglad znikania, bo dodajac musisz ustawic i tak w scen config zeby to dzialalo xdd
        // TAKZE OBIE METODY TRZEBA LAPAC W SCENE CONFIG ZDUPLIKOWANYCH SCENACH JAKOS OPTYMALNIE
        
        if (MainScene.SceneUuid.Equals(e.SceneUuid))
        {
            MainScene.RemoveSceneItem(e.SourceUuid);
        }
        else if (PreviewScene.SceneUuid.Equals(e.SceneUuid))
        {
            PreviewScene.RemoveSceneItem(e.SourceUuid);
        }
        
        foreach (var additionalScene in AdditionalScenes)
        {
            if (!additionalScene.SceneUuid.Equals(e.SceneUuid)) continue;
                
            additionalScene.RemoveSceneItem(e.SourceUuid);
        }
    }
    
    private async Task InitializeAsync()
    {
        GetSceneListResponseData? sceneResponse = await _obs.GetSceneListAsync();
        if (sceneResponse != null)
        {
            await _dispatcher.InvokeAsync(()=>
            {
                _scenes.Clear();
                foreach (SceneStub scene in sceneResponse.Scenes ?? [])
                {
                    _scenes.Add(SceneDto.Create(scene.SceneName, scene.SceneUuid));
                }
            });
        }

        GetVideoSettingsResponseData? settings = await _obs.GetVideoSettingsAsync();
        if (settings != null)
        {
            MainScene.SetBaseWidth((float)settings.BaseWidth);
            PreviewScene.SetBaseWidth((float)settings.BaseWidth);
        }

        GetCurrentProgramSceneResponseData? mainScene = await _obs.GetCurrentProgramSceneAsync();
        if (mainScene != null)
        {
            await MainScene.SetSceneItemsAsync(mainScene.SceneName ?? string.Empty, mainScene.SceneUuid ?? string.Empty, true);
        }
        
        StudioModeChanged();
    }
    
    private async Task OnOBSConnectedAsync()
    {
        await InitializeAsync();
        ObsConnected?.Invoke(this, EventArgs.Empty);
    }
    private void OnOBSDisconnected()
    {
        _dispatcher.Invoke(_scenes.Clear);
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
    private void OnStudioModeChanged(object? sender, EventArgs e) => StudioModeChanged();

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
    
    private void StudioModeChanged()
    {
        bool option = _obs.StudioMode;
        if (!option) return;
        
        PreviewScene.Clear();
    }
    
    private async Task CurrentMainSceneChanged(string sceneName, string sceneUuid)
    {
        bool isDuplicate = sceneUuid.Equals(MainScene.SceneUuid);
        _logger.Log($"Program scene: {sceneName}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

        await MainScene.SetSceneItemsAsync(sceneName, sceneUuid);
    }
    private async Task CurrentPreviewSceneChanged(string sceneName, string sceneUuid)
    {
        if (sceneName.Equals(PreviewScene.SceneName)) return;
        if (sceneName.Equals(MainScene.SceneName))
        {
            PreviewScene.Clear();
            SelectedSceneUpdated?.Invoke(this, MainScene.SceneName);
            return;
        }
        
        _logger.Log("Loading Preview scene: " + sceneName);
        
        await PreviewScene.SetSceneItemsAsync(sceneName, sceneUuid);
        SelectedSceneUpdated?.Invoke(this, PreviewScene.SceneName);
    }
    
    private async Task UpdateSceneItems(string sceneName, string sceneUuid)
    {
        if (sceneUuid.Equals(MainScene.SceneUuid))
        {
            await MainScene.SetSceneItemsAsync(sceneName, sceneUuid, true);
        }
        if (sceneUuid.Equals(PreviewScene.SceneUuid))
        {
            await PreviewScene.SetSceneItemsAsync(sceneName, sceneUuid, true);
        }
        
        foreach (var additionalScene in AdditionalScenes)
        {
            if (!additionalScene.SceneUuid.Equals(sceneUuid)) continue;
                
            await additionalScene.SetSceneItemsAsync(sceneName, sceneUuid, true);
        }
    }
    
    public async Task RefreshScenesPOVSAsync()
    {
        if (BusyWithOBS) return;
        BusyWithOBS = true;
        
        await MainScene.RefreshItems();
        await PreviewScene.RefreshItems();
        BusyWithOBS = false;
    }

    public void QueueUpdate(string sourceUuid, Dictionary<string, object> input)
    {
        JsonElement element = JsonSerializer.SerializeToElement(input);
        _obsUpdateBatcher.Queue(new SetInputSettingsRequestData(element, null, sourceUuid));
    }

    public async Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input)
        => await _obs.SetItemInputSettingsAsync(sourceUuid, input);
    public async Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid)
        => await _obs.GetInputSettingsAsync(sourceUuid);

    public IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type) => _playerRepository.GetPlayerByStreamName(name, type);
    
    public void AddAdditionalScene(Scene additionalScene) => AdditionalScenes.Add(additionalScene);
    public void RemoveAdditionalScene(Scene additionalScene) => AdditionalScenes.Remove(additionalScene);

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
            List<SceneItemStub> sceneItems = await _obs.GetSceneItemListAsync(sceneName, sceneUuid);
            
            foreach (SceneItemStub item in sceneItems)
            {
                if (item.ExtensionData == null) continue;
                
                string sourceType = item.ExtensionData[nameof(ExtensionDataType.sourceType)].ToString() ?? string.Empty;
                if (sourceType.Equals(nameof(SourceType.OBS_SOURCE_TYPE_SCENE)))
                {
                    item.ExtensionData[nameof(ExtensionDataType.inputKind)] = JsonSerializer.SerializeToElement(nameof(InputKind.group_source)); 
                    
                    List<SceneItemStub> groupItems = item.IsGroup == true ? await _obs.GetGroupSceneItemListAsync(item.SourceName, item.SourceUuid) : [];
                    foreach (SceneItemStub groupItem in groupItems)
                    {
                        if (groupItem.ExtensionData == null) continue;
                        
                        string groupItemKind = groupItem.ExtensionData![nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
                        if (string.IsNullOrEmpty(groupItemKind)) continue;

                        items.Add((groupItem, item));
                    }
                }
                
                string itemInputKind = item.ExtensionData[nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
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

    public IPointOfView? GetPointOfView(string sourceName)
    {
        foreach (SceneItem sceneItem in MainScene.SceneItems)
        {
            if (sceneItem is not PointOfView pov || !pov.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase)) continue;
            return pov;
        }

        foreach (SceneItem sceneItem in PreviewScene.SceneItems)
        {
            if (sceneItem is not PointOfView pov || !pov.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase)) continue;
            return pov;
        }
        
        return null;
    }
}