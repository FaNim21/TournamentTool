using System.Windows.Input;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public class UnSelectTriggeredEventArgs(bool cleaAll) : EventArgs
{
    public bool ClearAll { get; } = cleaAll;
}

public class SceneRuntimeViewModel : SceneCanvasViewModel, IScenePovInteractable
{
    public bool IsStudioModeSupported { get; }
    protected override bool InEditMode => false;

    public event EventHandler<UnSelectTriggeredEventArgs>? UnSelectTriggered;

    public ICommand RefreshPOVsCommand { get; private set; }
    public ICommand RefreshOBSCommand { get; private set; }
    public ICommand SwitchStudioModeCommand {  get; private set; }
    public ICommand StudioModeTransitionCommand { get; private set; }
    
    
    public SceneRuntimeViewModel(IObsController obs, ILoggingService logger, IDispatcherService dispatcher,
        IWindowService windowService, ISceneManager sceneManager, bool isStudioModeSupported = true) : 
        base(obs, logger, dispatcher, sceneManager)
    {
        IsStudioModeSupported = isStudioModeSupported;

        Setup(this, windowService);
        
        RefreshPOVsCommand = new AsyncRelayCommand(sceneManager.RefreshScenesPOVSAsync);
        RefreshOBSCommand = new RelayCommand(RefreshScenes);
        SwitchStudioModeCommand = new AsyncRelayCommand(OBS.SwitchStudioModeAsync);
        StudioModeTransitionCommand = new AsyncRelayCommand(StudioModeTransitionAsync);
        SelectedSceneChangedCommand = new AsyncRelayCommand<SceneDto>(OnSelectedSceneChangedAsync);
    }
    public override void OnEnable(object? parameter)
    {
        OBS.StudioModeChanged += OnStudioModeChanged;
        
        base.OnEnable(parameter);
    }
    public override bool OnDisable()
    {
        OBS.StudioModeChanged -= OnStudioModeChanged;
        
        return base.OnDisable();
    }
    
    public void RefreshScenes()
    {
        MainSceneViewModel.Refresh();
        PreviewSceneViewModel.Refresh();
    }
    
    private async Task StudioModeTransitionAsync(CancellationToken token)
    {
        if (string.IsNullOrEmpty(PreviewSceneViewModel.SceneName)) return;
        if (MainSceneViewModel.SceneName.Equals(PreviewSceneViewModel.SceneName)) return;
        
        await OBS.TransitionStudioModeAsync();
    }

    private void OnStudioModeChanged(object? sender, EventArgs e)
    {
        bool option = IsStudioModeSupported && OBS.StudioMode;

        OnPropertyChanged(nameof(StudioMode));
            
        MainSceneViewModel.SetStudioMode(option);
        MainSceneViewModel.UpdateItemsProportions();
            
        PreviewSceneViewModel.SetStudioMode(option);
        PreviewSceneViewModel.UpdateItemsProportions();

        if (!option) return;
            
        OnSelectedSceneUpdated(null, MainSceneViewModel.SceneName);
        PreviewSceneViewModel.Refresh();
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
    public void UnSelectItems(bool clearAll = false) => UnSelectTriggered?.Invoke(this, new UnSelectTriggeredEventArgs(clearAll));

    private async Task OnSelectedSceneChangedAsync(SceneDto? selectedScene, CancellationToken token)
    {
        if (selectedScene == null) return;
        if (_blockSetCurrentPreview)
        {
            _blockSetCurrentPreview = false;
            return;
        }
        
        await OBS.SetCurrentPreviewSceneAsync(selectedScene.Name);
    }
}