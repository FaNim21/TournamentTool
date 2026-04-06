using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Controller;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public class SceneViewModel : BaseViewModel
{
    private const float _studioFactor = 2.05f;
    
    private readonly Scene _scene;
    private readonly Lock _lock = new();
    protected SceneType Type { get; set; }

    public IScenePovInteractable Interactable { get; }
    public ISceneControllerViewModel SceneControllerViewModel { get; }
    
    protected ILoggingService Logger { get; }

    private ObservableCollection<SceneItemViewModel> _sceneItems = [];
    public ObservableCollection<SceneItemViewModel> SceneItems
    {
        get => _sceneItems; 
        set
        { 
            _sceneItems = value;
            OnPropertyChanged();
        }
    }

    public string SceneName => _scene.SceneName;
    public string SceneUuid => _scene.SceneUuid;

    private float _canvasWidth;
    public float CanvasWidth
    {
        get => _canvasWidth;
        private set => SetField(ref _canvasWidth, value);
    }
    
    private float _canvasHeight;
    public float CanvasHeight 
    {
        get => _canvasHeight;
        private set => SetField(ref _canvasHeight, value);
    }

    public float BaseWidth => _scene.BaseWidth;
    public float ProportionsRatio => BaseWidth / CanvasWidth;

    private bool _studioModeEnabled;
    public bool StudioModeEnabled 
    { 
        get => _studioModeEnabled; 
        private set
        {
            _studioModeEnabled = value;
            OnPropertyChanged();
        }
    }

    private float _fontSizeSceneName;
    public float FontSizeSceneName
    {
        get => _fontSizeSceneName;
        set
        {
            _fontSizeSceneName = value;
            OnPropertyChanged();
        }
    }

    public ICommand ClearPOVCommand { get; private set; }
    public ICommand RefreshPOVCommand { get; private set; }
    public ICommand ShowInfoWindowCommand { get; private set; }


    public SceneViewModel(Scene scene, SceneType type, IScenePovInteractable interactable, ISceneControllerViewModel sceneControllerViewModel, IWindowService windowService, 
        ILoggingService logger, IDispatcherService dispatcher) : base(dispatcher)
    {
        _scene = scene;
        Interactable = interactable;
        SceneControllerViewModel = sceneControllerViewModel;
        Logger = logger;

        Type = type;

        ClearPOVCommand = new RelayCommand<PointOfViewViewModel>(async pov => { await pov.ClearAsync(true); });
        RefreshPOVCommand = new RelayCommand<PointOfViewViewModel>(pov => { pov.RefreshCommand.Execute(null); });
        ShowInfoWindowCommand = new ShowPOVInfoWindowCommand(windowService, this, dispatcher);
    }
    public override void OnEnable(object? parameter)
    {
        _scene.ItemAdded += OnItemAdded;
        _scene.ItemRemoved += OnItemRemoved;
        _scene.ItemsCleared += OnItemsCleared;
        _scene.SceneRecreated += OnSceneRecreated;
        
        Refresh();
    }
    public override bool OnDisable()
    {
        _scene.ItemAdded -= OnItemAdded;
        _scene.ItemRemoved -= OnItemRemoved;
        _scene.ItemsCleared -= OnItemsCleared;
        _scene.SceneRecreated -= OnSceneRecreated;
        
        return base.OnDisable();
    }

    private void OnSceneRecreated(object? sender, Scene scene)
    {
        //TODO: 0 resetowanie view modelu sceny
    }
    private void OnItemsCleared(object? sender, EventArgs e) => Dispatcher.Invoke(SceneItems.Clear);
    private void OnItemAdded(object? sender, SceneItem item) => AddSceneItemAsync(item);
    private void OnItemRemoved(object? sender, SceneItem item) => RemoveSceneItem(item);

    public void SetStudioMode(bool option)
    {
        //TODO: 0 To nie dziala przez to, ze w momencie juz wywolania tego studio mode enabled jest juz zmienione przez scene manager
        if (option && !StudioModeEnabled)
        {
            CanvasWidth /= _studioFactor;
            CanvasHeight /= _studioFactor;
        }
        else if (!option && StudioModeEnabled)
        {
            CanvasWidth *= _studioFactor;
            CanvasHeight *= _studioFactor;
        }
        
        StudioModeEnabled = option;
        
        UpdateItemsProportions();
    }
    
    public void SetSceneItems(string sceneName, string sceneUuid, bool force = false)
    {
        if (string.IsNullOrEmpty(sceneName) && string.IsNullOrEmpty(sceneUuid)) return;
        if (SceneName.Equals(sceneName) && SceneUuid.Equals(sceneUuid) && !force) return;

        if (!SceneName.Equals(sceneName))
        {
            OnPropertyChanged(nameof(SceneName));
        }
        if (!SceneUuid.Equals(sceneUuid))
        {
            OnPropertyChanged(nameof(SceneUuid));
        }
        
        foreach (var sceneItem in SceneItems)
        {
            sceneItem.OnDestroy();
        }

        List<SceneItemViewModel> createdSceneItems = [];
        
        for (int i = 0; i < _scene.SceneItems.Count; i++)
        {
            SceneItem currentSceneItemData = _scene.SceneItems[i];
            SceneItemViewModel? sceneItem = CreateSceneItem(currentSceneItemData);
            if (sceneItem == null) continue;
            
            createdSceneItems.Add(sceneItem);
        }

        Dispatcher.Invoke(delegate
        {
            lock (_lock)
            {
                SceneItems.Clear();
                foreach (var item in createdSceneItems)
                {
                    SceneItems.Add(item);
                }
            }
        }, CustomDispatcherPriority.Background);
    }

    public void Refresh()
    {
        SetSceneItems(SceneName!, SceneUuid, true);
    }
    public async Task RefreshItems()
    {
        for (int i = 0; i < SceneItems.Count; i++)
        {
            await SceneItems[i].RefreshAsync();
        }
    }

    private SceneItemViewModel? CreateSceneItem(SceneItem item)
    {
        SceneItemViewModel? sceneItem = item switch
        {
            PointOfView pov => new PointOfViewViewModel(pov, Dispatcher, Logger),
            BrowserItem browser => new BrowserItemViewModel(browser, Dispatcher, Logger),
            TextItem text => new TextItemViewModel(text, Dispatcher, Logger),
            _ => null
        };
        
        if (sceneItem == null) return null;

        bool isDisplayed = !(!SceneControllerViewModel.InEditMode && sceneItem.InputKind != InputKind.tt_point_of_view);
        
        sceneItem.Initialize(SceneControllerViewModel.InEditMode, isDisplayed);
        sceneItem.Transform.UpdateProportions(ProportionsRatio);

        return sceneItem;
    }
    public void AddSceneItemAsync(SceneItem item)
    {
        SceneItemViewModel? sceneItem = CreateSceneItem(item);
        if (sceneItem == null) return;

        Dispatcher.Invoke(delegate
        {
            lock (_lock)
            {
                SceneItems.Add(sceneItem);
            }
        });
    }
    
    public void RemoveSceneItem(SceneItem itemData)
    {
        foreach (SceneItemViewModel sceneItemViewModel in SceneItems)
        {
            if (!sceneItemViewModel.SceneItem.Equals(itemData)) continue;
            
            RemoveSceneItem(sceneItemViewModel);
            break;
        }
    }
    public void RemoveSceneItem(SceneItemViewModel item)
    {
        Dispatcher.BeginInvoke(() =>
        {
            lock (_lock)
            {
                item.OnDestroy();
                SceneItems.Remove(item);
            }
        });
    }
    public void ClearSceneItems()
    {
        foreach (SceneItemViewModel item in SceneItems)
        {
            item.OnDestroy();
        }
        
        Dispatcher.Invoke(SceneItems.Clear);
    }

    public bool ExistInItems<T>(Func<T, bool> condition) where T : SceneItemViewModel
        => SceneItems.OfType<T>().Any(condition);
    public T? GetItem<T>(Func<T, bool> condition) where T : SceneItemViewModel
        => SceneItems.OfType<T>().FirstOrDefault(condition);
    
    public void ChangeSceneSize(float newWidth, float newHeight) 
    {
        if (StudioModeEnabled)
        {
            CanvasWidth = newWidth / _studioFactor;
            CanvasHeight = newHeight / _studioFactor;
        }
        else
        {
            CanvasWidth = newWidth;
            CanvasHeight = newHeight;
        }
        
        UpdateItemsProportions();
    }

    public void UpdateItemsProportions()
    {
        for (int i = 0; i < SceneItems.Count; i++)
        {
            var pov = SceneItems[i];
            pov.Transform.UpdateProportions(ProportionsRatio);
        }
    }
}