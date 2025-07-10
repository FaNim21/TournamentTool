using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using OBSStudioClient.Events;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.OBS;

public class SceneControllerViewmodel : BaseViewModel
{
    private readonly Lock _lock = new();
    
    public ControllerViewModel Controller { get; }
    public TournamentViewModel Tournament { get; }
    public ObsController OBS { get; }
    
    public Scene MainScene { get; }
    public PreviewScene PreviewScene { get; }

    private PointOfView? _currentChosenPOV;
    public PointOfView? CurrentChosenPOV
    {
        get => _currentChosenPOV;
        set
        {
            if (value == null)
                _currentChosenPOV?.UnFocus();
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

    public ICommand RefreshPOVsCommand { get; set; }
    public ICommand RefreshOBSCommand { get; set; }
    public ICommand SwitchStudioModeCommand {  get; set; }
    public ICommand StudioModeTransitionCommand { get; set; }

    public SceneControllerViewmodel(ControllerViewModel controller, ICoordinator coordinator, ObsController obs, TournamentViewModel tournament)
    {
        Controller = controller;
        OBS = obs;
        Tournament = tournament;

        MainScene = new Scene(this, coordinator);
        PreviewScene = new PreviewScene(this, coordinator);
        
        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
        RefreshOBSCommand = new RelayCommand(async () => { await RefreshScenes(); });
        SwitchStudioModeCommand = new RelayCommand(OBS.SwitchStudioMode);
        StudioModeTransitionCommand = new RelayCommand(() =>
        {
            if (MainScene.SceneName!.Equals(PreviewScene.SceneName)) return;
            OBS.TransitionStudioMode();
        });
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
            Task.Run(Initialize);
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
        await MainScene.GetCurrentSceneItems(mainScene);
        
        MainScene.RefreshItems();
    }

    private void UpdateView()
    {
        OnPropertyChanged(nameof(Connected));
        OnPropertyChanged(nameof(StudioMode));
    }

    private void OnOBSConnected()
    {
        UpdateView();
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
            case OBSConnectionState.Connected: OnOBSConnected(); break;
            case OBSConnectionState.Disconnected: OnOBSDisconnected(); break;
            case OBSConnectionState.Connecting:
            case OBSConnectionState.Disconnecting: break;
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
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
        }
    }
    private async void OnSceneTransitionStarted(object? sender, EventArgs e)
    {
        try
        {
            await SceneTransitionStarted();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
            Console.WriteLine(ex);
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
            await PreviewScene.GetCurrentSceneItems(MainScene.SceneName, true);
            await LoadPreviewScene(MainScene.SceneName);
        }
    }
    
    private async Task CurrentMainSceneChanged(string scene)
    {
        bool isDuplicate = scene.Equals(MainScene.SceneName);
        Trace.WriteLine($"Program scene: {scene}, duplicate: {isDuplicate}");
        if (isDuplicate) return;

        await MainScene.GetCurrentSceneItems(scene);
    }
    private async Task CurrentPreviewSceneChanged(string scene)
    {
        if (scene.Equals(PreviewScene.SceneName)) return;
        Trace.WriteLine("Loading Preview scene: " + scene);
        
        await LoadScenesForStudioMode(false);
        await PreviewScene.GetCurrentSceneItems(scene);
        await LoadPreviewScene(scene, true);
    }

    private async Task SceneTransitionStarted()
    {
        if (MainScene.SceneName!.Equals(PreviewScene.SceneName)) return;
        OBS.SetStartedTransition(true);

        Trace.WriteLine("Started Transition");
        string previewScene = PreviewScene.SceneName;
        string mainScene = MainScene.SceneName;

        await MainScene.GetCurrentSceneItems(previewScene);
        await PreviewScene.GetCurrentSceneItems(mainScene);

        UpdateSelectedScene(mainScene);
    }
    
    private async Task UpdateSceneItems(string scene)
    {
        if (scene.Equals(MainScene.SceneName))
        {
            await MainScene.GetCurrentSceneItems(scene, true);
            return;
        }

        await PreviewScene.GetCurrentSceneItems(scene, true);
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
        Trace.WriteLine($"loading preview - {sceneName}");

        UpdateSelectedScene(sceneName);

        if (isFromApi || SelectedScene.Equals(PreviewScene.SceneName)) return;
        await OBS.SetCurrentPreviewScene(SelectedScene);
    }
    
    private async Task RefreshScenesPOVS()
    { 
        await MainScene.RefreshPovs();
        await PreviewScene.RefreshPovs();
    }
    private async Task RefreshScenes()
    {
        if (!OBS.IsConnectedToWebSocket) return;
        
        Controller.TournamentViewModel.ClearPlayersFromPOVS();
        
        await MainScene.Refresh();
        await PreviewScene.Refresh();
    }
    
    private async Task LoadScenesForStudioMode(bool force = true)
    {
        await Task.Delay(50);
        var loadedScenes = await OBS.GetSceneList();

        lock (_lock)
        {
            if (!force && loadedScenes.Scenes.Length == Scenes.Count) return;

            Trace.WriteLine("Loading preview scenes list");
            Application.Current.Dispatcher.Invoke(Scenes.Clear);

            for (int i = loadedScenes.Scenes.Length - 1; i >= 0; i--)
            {
                var current = loadedScenes.Scenes[i];
                Application.Current.Dispatcher.Invoke(() => { Scenes.Add(current.SceneName); });
            }
        }
    }

    private void ClearScenes()
    {
        MainScene.ClearPovs();
        PreviewScene.ClearPovs();
    }
}