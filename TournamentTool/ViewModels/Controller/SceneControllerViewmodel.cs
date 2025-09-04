using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Controller;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;
using ConnectionState = TournamentTool.Modules.OBS.ConnectionState;

namespace TournamentTool.ViewModels.Controller;

public interface IScenePovInteractable
{
    void OnPOVClick(Modules.OBS.Scene scene, PointOfView clickedPov);
    void UnSelectItems(bool clearAll = false);
}

public interface ISceneController
{
    void ClearPlayersFromPovs();

    PlayerViewModel? GetPlayerByStreamName(string name, StreamType type);
    Task<(string?, int, StreamType)> GetBrowserURLStreamInfo(string sceneItemName);
    Task<(List<(SceneItem, SceneItem?)>, List<SceneItem>)> GetSceneItems(string scene);
}

/// <summary>
/// TODO: 1 MAKE IT SOLID for 0.13 update
/// </summary>
public class SceneControllerViewmodel : BaseViewModel, ISceneController, IScenePovInteractable
{
    private readonly Lock _lock = new();
    
    public ControllerViewModel Controller { get; }
    public TournamentViewModel Tournament { get; }
    public ILoggingService Logger { get; }
    public ObsController OBS { get; }

    public Modules.OBS.Scene MainScene { get; }
    public Modules.OBS.Scene PreviewScene { get; }

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

    public bool BusyWithOBS { get; private set; }

    public ICommand RefreshPOVsCommand { get; set; }
    public ICommand RefreshOBSCommand { get; set; }
    public ICommand SwitchStudioModeCommand {  get; set; }
    public ICommand StudioModeTransitionCommand { get; set; }
    public ICommand OnSceneResizeCommand { get; set; }

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


    public SceneControllerViewmodel(ControllerViewModel controller, IDialogWindow dialogWindow, ObsController obs, TournamentViewModel tournament, ILoggingService logger)
    {
        Controller = controller;
        OBS = obs;
        Tournament = tournament;
        Logger = logger;

        IPointOfViewOBSController povController = new PointOfViewOBSController(obs, tournament);
        MainScene = new Modules.OBS.Scene(SceneType.Main, this, this, povController, dialogWindow, logger);
        PreviewScene = new Modules.OBS.Scene(SceneType.Preview, this, this, povController, dialogWindow, logger);
        
        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
        RefreshOBSCommand = new RelayCommand(async () => { await RefreshScenes(); });
        SwitchStudioModeCommand = new RelayCommand(OBS.SwitchStudioMode);
        StudioModeTransitionCommand = new RelayCommand(() =>
        {
            if (MainScene.SceneName!.Equals(PreviewScene.SceneName) ||
                string.IsNullOrEmpty(PreviewScene.SceneName)) return;
            OBS.TransitionStudioMode();
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
        var settings = await OBS.GetVideoSettings();
        MainScene.CalculateProportionsRatio(settings.BaseWidth);
        PreviewScene.CalculateProportionsRatio(settings.BaseWidth);
        
        string mainScene = await OBS.GetCurrentProgramScene();
        await MainScene.SetSceneItems(mainScene, true);
        await StudioModeChanged();
    }

    private void OnSceneResize(Dimension dimension)
    {
        if (Math.Abs(ScenePreviewWidth - dimension.Width) < 1f &&
            Math.Abs(ScenePreviewHeight - dimension.Height) < 1f) return;

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
        Tournament.ClearPlayersFromPOVS();
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
    private async void OnSceneUpdateRequested(object? sender, SceneNameEventArgs e)
    {
        try
        {
            await UpdateSceneItems(e.SceneName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private async void OnCurrentProgramSceneChanged(object? sender, SceneNameEventArgs e)
    {
        try
        {
            await CurrentMainSceneChanged(e.SceneName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private async void OnCurrentPreviewSceneChanged(object? sender, SceneNameEventArgs e)
    {
        try
        {
            await CurrentPreviewSceneChanged(e.SceneName);
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
        var option = OBS.StudioMode;
        
        MainScene.SetStudioMode(option);
        PreviewScene.SetStudioMode(option);
        MainScene.RefreshItems();
        PreviewScene.RefreshItems();

        if (option)
        {
            await LoadScenesForStudioMode();
            PreviewScene.Clear();
            await LoadPreviewScene(MainScene.SceneName);
        }
    }
    
    private async Task CurrentMainSceneChanged(string scene)
    {
        bool isDuplicate = scene.Equals(MainScene.SceneName);
        Logger.Log($"Program scene: {scene}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

        await MainScene.SetSceneItems(scene);
    }
    private async Task CurrentPreviewSceneChanged(string scene)
    {
        if (scene.Equals(PreviewScene.SceneName)) return;
        if (scene.Equals(MainScene.SceneName))
        {
            PreviewScene.Clear();
            return;
        }
        Logger.Log("Loading Preview scene: " + scene);
        
        await LoadScenesForStudioMode(false);
        await PreviewScene.SetSceneItems(scene);
        await LoadPreviewScene(scene, true);
    }
    
    private async Task UpdateSceneItems(string scene)
    {
        if (scene.Equals(MainScene.SceneName))
        {
            await MainScene.SetSceneItems(scene, true);
            return;
        }

        await PreviewScene.SetSceneItems(scene, true);
    }

    private void UpdateSelectedScene(string sceneName)
    {
        Application.Current.Dispatcher.Invoke(() =>
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
        await MainScene.RefreshPovs();
        await PreviewScene.RefreshPovs();
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
        Tournament.ClearPlayersFromPOVS();
    }
    
    private async Task LoadScenesForStudioMode(bool force = true)
    {
        await Task.Delay(50);
        var loadedScenes = await OBS.GetSceneList();

        lock (_lock)
        {
            if (!force && loadedScenes.Scenes.Length == Scenes.Count) return;

            Logger.Log("Loading preview scenes list");
            Application.Current.Dispatcher.Invoke(Scenes.Clear);

            for (int i = loadedScenes.Scenes.Length - 1; i >= 0; i--)
            {
                var current = loadedScenes.Scenes[i];
                Application.Current.Dispatcher.Invoke(() => { Scenes.Add(current.SceneName); });
            }
        }
    }

    public void OnPOVClick(Modules.OBS.Scene scene, PointOfView clickedPov)
    {
        CurrentChosenPOV?.UnFocus();
        if (CurrentChosenPOV is { } && CurrentChosenPOV == clickedPov)
        {
            CurrentChosenPOV = null;
            return;
        }
        
        PointOfView? previousPOV = CurrentChosenPOV;
        CurrentChosenPOV = clickedPov;
        
        if (Controller.CurrentChosenPlayer == null)
        {
            if (previousPOV is { IsEmpty: true } && CurrentChosenPOV!.IsEmpty)
            {
                previousPOV.UnFocus();
            }
            else if (CurrentChosenPOV!.Swap(previousPOV))
            {
                CurrentChosenPOV = null;
            }
            
            CurrentChosenPOV?.Focus();
            return;
        }

        if (!scene.IsPlayerInPov(Controller.CurrentChosenPlayer.StreamDisplayInfo))
        {
            clickedPov.SetPOV(Controller.CurrentChosenPlayer);
        }
        else
        {
            var pov = scene.GetPlayerPov(Controller.CurrentChosenPlayer.StreamDisplayInfo.Name,
                Controller.CurrentChosenPlayer.StreamDisplayInfo.Type);
            if (pov != null)
            {
                CurrentChosenPOV!.Swap(pov);
                UnSelectItems();
                return;
            }
        }

        CurrentChosenPOV.UnFocus();
        UnSelectItems(true);
    }
    public void UnSelectItems(bool clearAll = false)
    {
        Controller.UnSelectItems(clearAll);
    }

    public PlayerViewModel? GetPlayerByStreamName(string name, StreamType type) => Tournament.GetPlayerByStreamName(name, type);
    public async Task<(string?, int, StreamType)> GetBrowserURLStreamInfo(string sceneItemName) => await OBS.GetBrowserURLStreamInfo(sceneItemName);
    public async Task<(List<(SceneItem, SceneItem?)>, List<SceneItem>)> GetSceneItems(string scene)
    {
        List<SceneItem> additionals = [];
        List<(SceneItem, SceneItem?)> povItems = [];
        
        try
        {
            //TODO: 1 TEMP z racji i tak reorganizacji kodu OBS do update 0.13
            SceneItem[] sceneItems = await OBS.GetSceneItemList(scene);

            foreach (var item in sceneItems)
            {
                if (item.SourceType == SourceType.OBS_SOURCE_TYPE_SCENE)
                {
                    SceneItem[] groupItems;
                    if (item.IsGroup == true)
                        groupItems = await OBS.GetGroupSceneItemList(item.SourceName);
                    else
                        groupItems = await OBS.GetSceneItemList(item.SourceName);
                    
                    foreach (var groupItem in groupItems)
                    {
                        if (string.IsNullOrEmpty(groupItem.InputKind)) continue;
                        if (CheckForAdditionals(additionals, groupItem)) continue;

                        if (groupItem.InputKind!.Equals("browser_source") &&
                            groupItem.SourceName.StartsWith(Tournament.FilterNameAtStartForSceneItems,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            povItems.Add((groupItem, item));
                        }
                    }
                }

                if (string.IsNullOrEmpty(item.InputKind)) continue;
                if (CheckForAdditionals(additionals, item)) continue;

                if (item.InputKind!.Equals("browser_source") &&
                    item.SourceName.StartsWith(Tournament.FilterNameAtStartForSceneItems,
                        StringComparison.OrdinalIgnoreCase))
                {
                    povItems.Add((item, null));
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        return (povItems, additionals);
    }
    private bool CheckForAdditionals(List<SceneItem> additionals, SceneItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.InputKind)) return false;

        if ((item.InputKind.Equals("browser_source") &&
             item.SourceName.StartsWith("head", StringComparison.OrdinalIgnoreCase)) ||
            item.InputKind.StartsWith("text"))
        {
            additionals.Add(item);
            return true;
        }

        return false;
    }

    private void ClearScenes()
    {
        MainScene.ClearPovs();
        MainScene.SetStudioMode(false);
        PreviewScene.ClearPovs();
        PreviewScene.SetStudioMode(false);
    }
}