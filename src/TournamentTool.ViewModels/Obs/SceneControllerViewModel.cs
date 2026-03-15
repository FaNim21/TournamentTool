using System.Collections.ObjectModel;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Events;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Obs.Items;
using ConnectionState = TournamentTool.Services.Controllers.ConnectionState;

namespace TournamentTool.ViewModels.Obs;

public class UnSelectTriggeredEventArgs(bool cleaAll) : EventArgs
{
    public bool ClearAll { get; } = cleaAll;
}

public interface IScenePovInteractable
{
    Task OnPOVClickAsync(Scene scene, PointOfView clickedPov);
    void UnSelectItems(bool clearAll = false);
}

public interface ISceneController
{
    public bool InEditMode { get; }
    
    Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input);
    Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid);
    
    IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type);
    Task<List<(SceneItemStub, SceneItemStub?)>> GetSceneItemsAsync(string sceneName, string sceneUuid);

    void RegisterBinding(SceneItemViewModel sceneItem);
    void RegisterSchema(BindingSchema schema);
    Task PublishAsync(BindingKey key, object value);
}

public class SceneControllerViewModel : BaseViewModel, ISceneController, IScenePovInteractable
{
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IBindingEngine _bindingEngine;
    private readonly Lock _lock = new();
    
    public ILoggingService Logger { get; }
    public IObsController OBS { get; }

    public Scene MainScene { get; }
    public Scene PreviewScene { get; }

    public IPlayer? CurrentChosenPlayer { get; set; }

    private PointOfView? _currentChosenPOV;
    public PointOfView? CurrentChosenPOV
    {
        get => _currentChosenPOV;
        set
        {
            if (value == null) _currentChosenPOV?.UnFocus();
            
            _currentChosenPOV = value;
            OnPropertyChanged(nameof(CurrentChosenPOV));
        }
    }

    private ObservableCollection<string> _scenes = [];
    public ObservableCollection<string> Scenes
    {
        get => _scenes;
        set
        {
            if (_scenes == value) return;
            _scenes = value;
            OnPropertyChanged(nameof(Scenes));
        }
    }
    
    private string _selectedScene = string.Empty;
    public string SelectedScene
    {
        get => _selectedScene;
        set
        {
            if (_selectedScene == value) return;
            _ = LoadPreviewScene(value);
        }
    }

    public bool StudioMode => OBS.StudioMode;
    public bool Connected => OBS.IsConnectedToWebSocket;

    private float _scenePreviewHeight = 240f;
    public float ScenePreviewHeight
    {
        get => _scenePreviewHeight;
        set
        {
            _scenePreviewHeight = value;
            OnPropertyChanged(nameof(ScenePreviewHeight));
        }
    }

    private float _scenePreviewWidth = 426f;
    public float ScenePreviewWidth
    {
        get => _scenePreviewWidth;
        set
        {
            _scenePreviewWidth = value;
            OnPropertyChanged(nameof(ScenePreviewWidth));
        }
    }

    public bool BusyWithOBS { get; private set; }
    public bool IsStudioModeSupported { get; set; } = true;
    public bool InEditMode { get; }

    public event EventHandler<UnSelectTriggeredEventArgs>? UnSelectTriggered;

    public ICommand RefreshPOVsCommand { get; private set; }
    public ICommand RefreshOBSCommand { get; private set; }
    public ICommand SwitchStudioModeCommand {  get; private set; }
    public ICommand StudioModeTransitionCommand { get; private set; }
    public ICommand OnSceneResizeCommand { get; private set; }
    

    public SceneControllerViewModel(IObsController obs, ITournamentPlayerRepository playerRepository, ILoggingService logger, IBindingEngine bindingEngine,
        ISettingsProvider settingsProvider, IDispatcherService dispatcher, IWindowService windowService, bool inEditMode = true) : base(dispatcher)
    {
        _playerRepository = playerRepository;
        _bindingEngine = bindingEngine;
        OBS = obs;
        Logger = logger;
        InEditMode = inEditMode;
        
        AppCache appCache = settingsProvider.Get<AppCache>();
        MainScene = new Scene(SceneType.Main, this, this, windowService, logger, dispatcher, appCache);
        PreviewScene = new Scene(SceneType.Preview, this, this, windowService, logger, dispatcher, appCache);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
        RefreshOBSCommand = new RelayCommand(async () => { await RefreshScenes(); });
        SwitchStudioModeCommand = new RelayCommand(async () => { await OBS.SwitchStudioMode(); });
        StudioModeTransitionCommand = new RelayCommand(async () =>
        {
            if (MainScene.SceneName!.Equals(PreviewScene.SceneName) ||
                string.IsNullOrEmpty(PreviewScene.SceneName)) return;
            await OBS.TransitionStudioModeAsync();
        });
        OnSceneResizeCommand = new RelayCommand<Dimension>(OnSceneResize);
    }

    public override void OnEnable(object? parameter)
    {
        OBS.SceneItemUpdateRequested += OnSceneUpdateRequested;
        OBS.ConnectionStateChanged += OnConnectionStateChanged;
        OBS.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
        OBS.CurrentPreviewSceneChanged += OnCurrentPreviewSceneChanged;
        OBS.SceneTransitionStarted += OnSceneTransitionStarted;
        OBS.StudioModeChanged += OnStudioModeChanged;

        if (Connected)
        {
            OnOBSConnected();
        }
        else
        {
            OnOBSDisconnected();
        }
    }
    public override bool OnDisable()
    {
        OBS.SceneItemUpdateRequested -= OnSceneUpdateRequested;
        OBS.ConnectionStateChanged -= OnConnectionStateChanged;
        OBS.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
        OBS.CurrentPreviewSceneChanged -= OnCurrentPreviewSceneChanged;
        OBS.SceneTransitionStarted -= OnSceneTransitionStarted;
        OBS.StudioModeChanged -= OnStudioModeChanged;
        
        MainScene.Clear();
        PreviewScene.Clear();
        
        SelectedScene = string.Empty;
        Scenes.Clear();
        
        return base.OnDisable();
    }

    private async Task Initialize()
    {
        GetVideoSettingsResponseData? settings = await OBS.GetVideoSettings();
        if (settings == null) return;
        
        MainScene.CalculateProportionsRatio((float)settings.BaseWidth);
        PreviewScene.CalculateProportionsRatio((float)settings.BaseWidth);
        
        GetCurrentProgramSceneResponseData? mainScene = await OBS.GetCurrentProgramScene();
        if (mainScene == null) return;
        
        await MainScene.SetSceneItemsAsync(mainScene.SceneName ?? string.Empty, mainScene.SceneUuid ?? string.Empty, true);
        await StudioModeChanged();
    }

    private void OnSceneResize(Dimension dimension)
    {
        if (Math.Abs(ScenePreviewWidth - dimension.Width) < 1f &&
            Math.Abs(ScenePreviewHeight - dimension.Height) < 1f) return;

        ResizeScene(dimension);
    }
    public void ResizeScene(Dimension dimension)
    {
        ScenePreviewWidth = dimension.Width;
        ScenePreviewHeight = dimension.Height;
        
        MainScene.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        PreviewScene.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
    }
    
    private void UpdateView()
    {
        MainScene.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        PreviewScene.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        OnPropertyChanged(nameof(Connected));
        OnPropertyChanged(nameof(StudioMode));
    }

    private void OnOBSConnected()
    {
        UpdateView();
        Task.Run(Initialize);
    }
    private void OnOBSDisconnected()
    {
        UpdateView();
        ClearPlayersFromPovs();
        ClearScenes();
    }

    private void OnConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case ConnectionState.Connected: OnOBSConnected(); break;
            case ConnectionState.Disconnected: OnOBSDisconnected(); break;
            case ConnectionState.Connecting:
            case ConnectionState.Disconnecting: break;
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
            Logger.Error(ex);
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
            Logger.Error(ex);
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
            Logger.Error(ex);
        }
    }
    private void OnSceneTransitionStarted(object? sender, EventArgs e)
    {
        if (MainScene.SceneName!.Equals(PreviewScene.SceneName)) return;
        OBS.SetStartedTransition(true);
        Logger.Log("Started Transition");
        
        MainScene.Swap(PreviewScene);
        UpdateSelectedScene(PreviewScene.SceneName);
    }
    private async void OnStudioModeChanged(object? sender, EventArgs e)
    {
        try
        {
            await StudioModeChanged();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private async Task StudioModeChanged()
    {
        OnPropertyChanged(nameof(StudioMode));
        bool option = IsStudioModeSupported && OBS.StudioMode;
        
        MainScene.SetStudioMode(option);
        PreviewScene.SetStudioMode(option);
        MainScene.UpdateItemsProportions();
        PreviewScene.UpdateItemsProportions();

        if (option)
        {
            await LoadScenesForStudioMode();
            PreviewScene.Clear();
            await LoadPreviewScene(MainScene.SceneName);
        }
    }
    
    private async Task CurrentMainSceneChanged(string sceneName, string sceneUuid)
    {
        bool isDuplicate = sceneUuid.Equals(MainScene.SceneUuid);
        Logger.Log($"Program scene: {sceneName}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

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
        Logger.Log("Loading Preview scene: " + sceneName);
        
        await LoadScenesForStudioMode(false);
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

    private void UpdateSelectedScene(string sceneName)
    {
        Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < Scenes.Count; i++)
            {
                var current = Scenes[i];
                if (!current.Equals(sceneName)) continue;
                
                _selectedScene = current;
                OnPropertyChanged(nameof(SelectedScene));
            }
        });
    }
    
    public async Task LoadPreviewScene(string sceneName, bool isFromApi = false)
    {
        if(!OBS.IsConnectedToWebSocket || string.IsNullOrEmpty(sceneName)) return;
        Logger.Log($"loading preview - {sceneName}");

        UpdateSelectedScene(sceneName);

        if (isFromApi || SelectedScene.Equals(PreviewScene.SceneName)) return;
        await OBS.SetCurrentPreviewScene(SelectedScene);
    }
    
    private async Task RefreshScenesPOVS()
    {
        if (BusyWithOBS) return;
        BusyWithOBS = true;
        
        await MainScene.RefreshItems();
        await PreviewScene.RefreshItems();
        BusyWithOBS = false;
    }
    private async Task RefreshScenes()
    {
        if (!OBS.IsConnectedToWebSocket || BusyWithOBS) return;
        BusyWithOBS = true;
        
        ClearPlayersFromPovs();
        
        await MainScene.Refresh();
        await PreviewScene.Refresh();
        BusyWithOBS = false;
    }

    public void ClearPlayersFromPovs()
    {
        foreach (var player in _playerRepository.Players)
        {
            player.ClearPOVDependencies();
        }
    }

    public async Task SetItemInputSettingsAsync(string sourceUuid, Dictionary<string, object> input)
        => await OBS.SetItemInputSettingsAsync(sourceUuid, input);
    public async Task<GetInputSettingsResponseData?> GetItemInputSettingsAsync(string sourceUuid)
        => await OBS.GetInputSettingsAsync(sourceUuid);

    private async Task LoadScenesForStudioMode(bool force = true)
    {
        await Task.Delay(50);
        GetSceneListResponseData? loadedScenes = await OBS.GetSceneList();
        if (loadedScenes == null) return;
        if (loadedScenes.Scenes == null) return;

        lock (_lock)
        {
            if (!force && loadedScenes.Scenes.Count == Scenes.Count) return;

            Logger.Log("Loading preview scenes list");
            Dispatcher.Invoke(Scenes.Clear);

            for (int i = loadedScenes.Scenes.Count - 1; i >= 0; i--)
            {
                var current = loadedScenes.Scenes[i];
                Dispatcher.Invoke(() => { Scenes.Add(current.SceneName ?? string.Empty); });
            }
        }
    }

    public async Task OnPOVClickAsync(Scene scene, PointOfView clickedPov)
    {
        CurrentChosenPOV?.UnFocus();
        if (CurrentChosenPOV is { } && CurrentChosenPOV == clickedPov)
        {
            CurrentChosenPOV = null;
            return;
        }
        
        PointOfView? previousPOV = CurrentChosenPOV;
        CurrentChosenPOV = clickedPov;
        
        if (CurrentChosenPlayer == null)
        {
            if (previousPOV is { IsEmpty: true } && CurrentChosenPOV!.IsEmpty)
            {
                previousPOV.UnFocus();
            }
            else if (await CurrentChosenPOV!.SwapAsync(previousPOV))
            {
                CurrentChosenPOV = null;
            }
            
            CurrentChosenPOV?.Focus();
            return;
        }

        PointOfView? pov = scene.GetItem<PointOfView>(p => p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo));
        if (pov != null)
        {
            await CurrentChosenPOV!.SwapAsync(pov);
            UnSelectItems();
            return;
        }

        await clickedPov.SetPOVAsync(CurrentChosenPlayer);

        CurrentChosenPOV.UnFocus();
        UnSelectItems(true);
    }
    
    public void UnSelectItems(bool clearAll = false) 
        => UnSelectTriggered?.Invoke(this, new UnSelectTriggeredEventArgs(clearAll));

    public IPlayerViewModel? GetPlayerByStreamName(string name, StreamType type) 
        => _playerRepository.GetPlayerByStreamName(name, type);

    public void RegisterBinding(SceneItemViewModel sceneItem) 
        => _bindingEngine.RegisterTarget(sceneItem.BindingKey, sceneItem);
    public void RegisterSchema(BindingSchema schema) 
        => _bindingEngine.RegisterSchema(schema);
    public async Task PublishAsync(BindingKey key, object value) 
        => await _bindingEngine.PublishAsync(key, value);

    public async Task<List<(SceneItemStub, SceneItemStub?)>> GetSceneItemsAsync(string sceneName, string sceneUuid)
    {
        List<(SceneItemStub, SceneItemStub?)> items = [];

        if (string.IsNullOrEmpty(sceneUuid)) return items;
        
        try
        {
            List<SceneItemStub> sceneItems = await OBS.GetSceneItemList(sceneName, sceneUuid);

            foreach (SceneItemStub item in sceneItems)
            {
                if (item.ExtensionData == null) continue;
                
                string itemInputKind = item.ExtensionData![nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
                
                string sourceType = item.ExtensionData[nameof(ExtensionDataType.sourceType)].ToString() ?? string.Empty;
                if (sourceType.Equals(nameof(SourceType.OBS_SOURCE_TYPE_SCENE)))
                {
                    List<SceneItemStub> groupItems = item.IsGroup == true ? await OBS.GetGroupSceneItemList(item.SourceName!) : [];
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
            Logger.Error(ex);
        }

        return items;
    }
    
    private void ClearScenes()
    {
        MainScene.ClearSceneItems();
        MainScene.SetStudioMode(false);
        PreviewScene.ClearSceneItems();
        PreviewScene.SetStudioMode(false);
    }
}