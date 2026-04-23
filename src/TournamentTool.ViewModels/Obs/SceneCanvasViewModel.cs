using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public abstract class SceneCanvasViewModel : BaseViewModel
{
    protected ILoggingService Logger { get; }
    protected IObsController OBS { get; }
    protected ISceneManager SceneManager { get; }

    public SceneViewModel MainSceneViewModel { get; private set; } = SceneViewModel.Empty();
    public SceneViewModel PreviewSceneViewModel { get; private set; } = SceneViewModel.Empty();

    protected abstract bool InEditMode { get; }
    
    public IPlayer? CurrentChosenPlayer { get; set; }

    private PointOfViewViewModel? _currentChosenPOV;
    public PointOfViewViewModel? CurrentChosenPOV
    {
        get => _currentChosenPOV;
        set
        {
            if (value == null) _currentChosenPOV?.UnFocus();
            
            _currentChosenPOV = value;
            OnPropertyChanged();
        }
    }

    public ReadOnlyObservableCollection<SceneDto> Scenes { get; }
    
    private SceneDto _selectedScene = SceneDto.Empty();
    public SceneDto SelectedScene
    {
        get => _selectedScene;
        set
        {
            _selectedScene = value;
            OnPropertyChanged();
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
            OnPropertyChanged();
        }
    }

    private float _scenePreviewWidth = 426f;
    public float ScenePreviewWidth
    {
        get => _scenePreviewWidth;
        set
        {
            _scenePreviewWidth = value;
            OnPropertyChanged();
        }
    }
    
    public ICommand SelectedSceneChangedCommand { get; set; } = new RelayCommand(() => {});
    public ICommand OnSceneResizeCommand { get; private set; }
    
    protected bool _blockSetCurrentPreview;
    
    
    protected SceneCanvasViewModel(IObsController obs, ILoggingService logger, IDispatcherService dispatcher, ISceneManager sceneManager) : base(dispatcher)
    {
        OBS = obs;
        SceneManager = sceneManager;
        Logger = logger;
        
        Scenes = sceneManager.Scenes;
        
        OnSceneResizeCommand = new RelayCommand<Dimension>(OnSceneResize);
    }
    public override void OnEnable(object? parameter)
    {
        SceneManager.ObsConnected += OnOBSConnected;
        SceneManager.ObsDisconnected += OnOBSDisconnected;
        SceneManager.SelectedSceneUpdated += OnSelectedSceneUpdated;
        
        MainSceneViewModel.OnEnable(null);
        PreviewSceneViewModel.OnEnable(null);
        
        if (Connected)
            OnOBSConnected(null, EventArgs.Empty);
        else
            OnOBSDisconnected(null, EventArgs.Empty);
        
    }
    public override bool OnDisable()
    {
        SceneManager.ObsConnected -= OnOBSConnected;
        SceneManager.ObsDisconnected -= OnOBSDisconnected;
        SceneManager.SelectedSceneUpdated -= OnSelectedSceneUpdated;
        
        MainSceneViewModel.OnDisable();
        PreviewSceneViewModel.OnDisable();
        
        return base.OnDisable();
    }
    
    protected void Setup(IScenePovInteractable? povInteractable, IWindowService windowService)
    {
        MainSceneViewModel = new SceneViewModel(SceneManager.MainScene, SceneType.Main, InEditMode, povInteractable, windowService, Logger, Dispatcher);
        PreviewSceneViewModel = new SceneViewModel(SceneManager.PreviewScene, SceneType.Preview, InEditMode, povInteractable, windowService, Logger, Dispatcher);
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
        
        MainSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        PreviewSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
    }
    
    private void OnOBSConnected(object? sender, EventArgs e)
    {
        UpdateView();
    }
    private void OnOBSDisconnected(object? sender, EventArgs e)
    {
        UpdateView();
        ClearScenes();
    }
    
    private void UpdateView()
    {
        MainSceneViewModel.SetStudioMode(StudioMode);
        PreviewSceneViewModel.SetStudioMode(StudioMode);
        
        MainSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        PreviewSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        
        OnPropertyChanged(nameof(Connected));
        OnPropertyChanged(nameof(StudioMode));

        OnSelectedSceneUpdated(null, StudioMode ? PreviewSceneViewModel.SceneName : MainSceneViewModel.SceneName);
    }
    
    protected void OnSelectedSceneUpdated(object? sender, string sceneName)
    {
        if (SelectedScene.Name.Equals(sceneName)) return;
        
        for (int i = 0; i < Scenes.Count; i++)
        {
            SceneDto current = Scenes[i];
            if (!current.Name!.Equals(sceneName)) continue;
                
            Dispatcher.Invoke(() =>
            {
                _blockSetCurrentPreview = true;
                SelectedScene = current;
            });
            break;
        }
    }

    protected void ClearScenes()
    {
        MainSceneViewModel.ClearSceneItems();
        MainSceneViewModel.SetStudioMode(false);
        PreviewSceneViewModel.ClearSceneItems();
        PreviewSceneViewModel.SetStudioMode(false);
    }
}