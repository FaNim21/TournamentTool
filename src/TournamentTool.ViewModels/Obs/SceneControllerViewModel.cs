using System.Collections.ObjectModel;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
using ObsWebSocket.Core.Protocol.Responses;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Obs.Items;
using ConnectionState = TournamentTool.Services.Obs.ConnectionState;

namespace TournamentTool.ViewModels.Obs;

public class UnSelectTriggeredEventArgs(bool cleaAll) : EventArgs
{
    public bool ClearAll { get; } = cleaAll;
}

public interface IScenePovInteractable
{
    Task OnPOVClickAsync(SceneViewModel sceneViewModel, PointOfViewViewModel clickedPov);
    void UnSelectItems(bool clearAll = false);
}

public interface ISceneControllerViewModel
{
    public bool InEditMode { get; }
}

public class SceneControllerViewModel : BaseViewModel, ISceneControllerViewModel, IScenePovInteractable
{
    private readonly ISceneManager _sceneManager;
    private readonly Lock _lock = new();
    
    public ILoggingService Logger { get; }
    public IObsController OBS { get; }

    public SceneViewModel MainSceneViewModel { get; }
    public SceneViewModel PreviewSceneViewModel { get; }

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
            
            if (_selectedScene == value) return;
            
            _ = _sceneManager.LoadPreviewScene(value.Name ?? string.Empty);
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
    
    public bool IsStudioModeSupported { get; }
    public bool InEditMode { get; }

    public event EventHandler<UnSelectTriggeredEventArgs>? UnSelectTriggered;

    public ICommand RefreshPOVsCommand { get; private set; }
    public ICommand RefreshOBSCommand { get; private set; }
    public ICommand SwitchStudioModeCommand {  get; private set; }
    public ICommand StudioModeTransitionCommand { get; private set; }
    public ICommand OnSceneResizeCommand { get; private set; }

    private readonly Domain.Entities.Settings _settings;
    
    /// <summary>
    /// Nowa struktura:
    /// - Scene controller, dzialajacy jako singleton ciagle w tle po polaczeniu sie z obsem do trzymania informacji o tym co sie dzieje w scenach,
    /// - Scene jako model ze wszystkimi informacjami ze sceney, czyli scene items, itp itd i wtedy SceneViewModel wyswietla scene i itemy z niej,
    /// - Nie wiem czy robic SceneItem wrapper, tutaj troche logiki juz zawarte jest, wiec nie wiem jak to zalatwic,
    /// - Scene controller dodam jeszcze powinien miec info o wszystkich scenach i aktywnie aktualizowac dane tak zeby view model mogl z niego czerpac i tez
    ///   byc aktywny przy bindingach i aktualizacji danych z serwisem do batch udpate'ow itemow
    /// </summary>
    public SceneControllerViewModel(IObsController obs, ILoggingService logger, ISettingsProvider settingsProvider, IDispatcherService dispatcher,
        IWindowService windowService, ISceneManager sceneManager, bool inEditMode = true, bool isStudioModeSupported = true) : base(dispatcher)
    {
        _sceneManager = sceneManager;
        OBS = obs;
        Logger = logger;
        
        InEditMode = inEditMode;
        IsStudioModeSupported = isStudioModeSupported;
        
        _settings = settingsProvider.Get<Domain.Entities.Settings>();
        Scenes = sceneManager.Scenes;
        
        //TODO: 0 Tu trzeba przydzielic modele scene do view modeli
        MainSceneViewModel = new SceneViewModel(sceneManager.MainScene, SceneType.Main, this, this, windowService, logger, dispatcher);
        PreviewSceneViewModel = new SceneViewModel(sceneManager.PreviewScene, SceneType.Preview, this, this, windowService, logger, dispatcher);

        RefreshPOVsCommand = new RelayCommand(async () => { await sceneManager.RefreshScenesPOVS(); });
        RefreshOBSCommand = new RelayCommand(RefreshScenes);
        SwitchStudioModeCommand = new RelayCommand(async () => { await OBS.SwitchStudioMode(); });
        StudioModeTransitionCommand = new RelayCommand(async () =>
        {
            if (MainSceneViewModel.SceneName!.Equals(PreviewSceneViewModel.SceneName) ||
                string.IsNullOrEmpty(PreviewSceneViewModel.SceneName)) return;
            await OBS.TransitionStudioModeAsync();
        });
        OnSceneResizeCommand = new RelayCommand<Dimension>(OnSceneResize);
    }
    public override void OnEnable(object? parameter)
    {
        _sceneManager.ObsConnected += OnOBSConnected;
        _sceneManager.ObsDisconnected += OnOBSDisconnected;
        _sceneManager.SelectedSceneUpdated += OnSelectedSceneUpdated;
        
        OBS.StudioModeChanged += OnStudioModeChanged;

        MainSceneViewModel.OnEnable(null);
        PreviewSceneViewModel.OnEnable(null);
        
        if (Connected)
            OnOBSConnected(null, EventArgs.Empty);
        else
            OnOBSDisconnected(null, EventArgs.Empty);
    }
    public override bool OnDisable()
    {
        _sceneManager.ObsConnected -= OnOBSConnected;
        _sceneManager.ObsDisconnected -= OnOBSDisconnected;
        _sceneManager.SelectedSceneUpdated -= OnSelectedSceneUpdated;
        
        OBS.StudioModeChanged -= OnStudioModeChanged;
        
        MainSceneViewModel.OnDisable();
        PreviewSceneViewModel.OnDisable();
        
        return base.OnDisable();
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
    
    public void RefreshScenes()
    {
        MainSceneViewModel.Refresh();
        PreviewSceneViewModel.Refresh();
    }
    
    private void UpdateView()
    {
        MainSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        PreviewSceneViewModel.ChangeSceneSize(ScenePreviewWidth, ScenePreviewHeight);
        OnPropertyChanged(nameof(Connected));
        OnPropertyChanged(nameof(StudioMode));
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

    private async void OnStudioModeChanged(object? sender, EventArgs e)
    {
        try
        {
            bool option = IsStudioModeSupported && OBS.StudioMode;

            OnPropertyChanged(nameof(StudioMode));
            
            MainSceneViewModel.SetStudioMode(option);
            MainSceneViewModel.UpdateItemsProportions();
            
            PreviewSceneViewModel.SetStudioMode(option);
            PreviewSceneViewModel.UpdateItemsProportions();

            if (!option) return;
            
            OnSelectedSceneUpdated(null, MainSceneViewModel.SceneName);
            // PreviewSceneViewModel.Clear();
            await _sceneManager.LoadPreviewScene(MainSceneViewModel.SceneName);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private void OnSelectedSceneUpdated(object? sender, string sceneName)
    {
        for (int i = 0; i < Scenes.Count; i++)
        {
            var current = Scenes[i];
            if (!current.Name!.Equals(sceneName)) continue;
                
            Dispatcher.Invoke(() =>
            {
                _selectedScene = current;
                OnPropertyChanged(nameof(SelectedScene));
            });
            break;
        }
    }

    public async Task OnPOVClickAsync(SceneViewModel sceneViewModel, PointOfViewViewModel clickedPov)
    {
        CurrentChosenPOV?.UnFocus();
        if (CurrentChosenPOV is { } && CurrentChosenPOV == clickedPov)
        {
            CurrentChosenPOV = null;
            return;
        }
        
        PointOfViewViewModel? previousPOV = CurrentChosenPOV;
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

        PointOfViewViewModel? pov = sceneViewModel.GetItem<PointOfViewViewModel>(p => p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo));
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

    private void ClearScenes()
    {
        MainSceneViewModel.ClearSceneItems();
        MainSceneViewModel.SetStudioMode(false);
        PreviewSceneViewModel.ClearSceneItems();
        PreviewSceneViewModel.SetStudioMode(false);
    }
}