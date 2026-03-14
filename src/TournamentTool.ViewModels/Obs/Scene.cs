using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Controller;
using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

public enum SceneType
{
    Main,
    Preview
}

public interface IScene
{
    string SceneName { get; }
    string SceneUuid { get; }

    bool ExistInItems<T>(Func<T, bool> condition) where T : SceneItemViewModel;
    T? GetItem<T>(Func<T, bool> condition) where T : SceneItemViewModel;
}

public class Scene : BaseViewModel, IScene
{
    private readonly AppCache _appCache;
    private readonly Lock _lock = new();
    protected SceneType Type { get; set; }

    public IScenePovInteractable Interactable { get; }
    public ISceneController SceneController { get; }
    
    protected ILoggingService Logger { get; }

    private ObservableCollection<SceneItemViewModel> _sceneItems = [];
    public ObservableCollection<SceneItemViewModel> SceneItems
    {
        get => _sceneItems; 
        set
        { 
            _sceneItems = value;
            OnPropertyChanged(nameof(SceneItems));
        }
    }

    private string _sceneName = string.Empty;
    public string SceneName
    {
        get => _sceneName;
        set
        {
            _sceneName = value;
            OnPropertyChanged(nameof(SceneName));
        }
    }
    
    private string _sceneUuid = string.Empty;
    public string SceneUuid
    {
        get => _sceneUuid;
        set
        {
            _sceneUuid = value;
            OnPropertyChanged(nameof(SceneUuid));
        }
    }

    private float _canvasWidth;
    public float CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            _canvasWidth = value;
            OnPropertyChanged(nameof(CanvasWidth));
        }
    }

    private float _canvasHeight;
    public float CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            _canvasHeight = value;
            OnPropertyChanged(nameof(CanvasHeight));
        }
    }

    private float _fontSizeSceneName;
    public float FontSizeSceneName
    {
        get => _fontSizeSceneName;
        set
        {
            _fontSizeSceneName = value;
            OnPropertyChanged(nameof(FontSizeSceneName));
        }
    }

    public float BaseWidth {  get; set; } 
    public float ProportionsRatio => BaseWidth / CanvasWidth;

    public bool StudioModeEnabled { get; private set; }

    public ICommand ClearPOVCommand { get; private set; }
    public ICommand RefreshPOVCommand { get; private set; }
    public ICommand ShowInfoWindowCommand { get; private set; }

    private const float _studioFactor = 2.05f;


    public Scene(SceneType type, IScenePovInteractable interactable, ISceneController sceneController, IWindowService windowService, 
        ILoggingService logger, IDispatcherService dispatcher, AppCache appCache) : base(dispatcher)
    {
        Interactable = interactable;
        SceneController = sceneController;
        Logger = logger;
        _appCache = appCache;

        Type = type;

        ClearPOVCommand = new RelayCommand<PointOfView>(async pov => { await pov.ClearAsync(true); });
        RefreshPOVCommand = new RelayCommand<PointOfView>(pov => { pov.RefreshCommand.Execute(null); });
        ShowInfoWindowCommand = new ShowPOVInfoWindowCommand(windowService, this, dispatcher);
    }

    public void Swap(Scene other)
    {
        var povs = other.SceneItems;
        float baseWidth = other.BaseWidth;
        string sceneName = other.SceneName;

        other.SceneName = SceneName;
        other.SceneItems = SceneItems;
        other.BaseWidth = BaseWidth;

        SceneName = sceneName;
        SceneItems = povs;
        BaseWidth = baseWidth;
    }

    public void SetStudioMode(bool option)
    {
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
        
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        UpdateItemsProportions();
    } 

    public async Task SetSceneItemsAsync(string sceneName, string sceneUuid, bool force = false)
    {
        if (string.IsNullOrEmpty(sceneName) && string.IsNullOrEmpty(sceneUuid)) return;
        if (sceneName.Equals(SceneName) && sceneUuid.Equals(SceneUuid) && !force) return;

        SceneName = sceneName;
        SceneUuid = sceneUuid;
        
        foreach (var sceneItem in SceneItems)
        {
            sceneItem.OnDestroy();
        }

        List<(SceneItemStub, SceneItemStub?)> items = await SceneController.GetSceneItemsAsync(sceneName, sceneUuid);
        List<SceneItemViewModel> createdSceneItems = [];
        
        for (int i = items.Count - 1; i >= 0; i--)
        {
            (SceneItemStub item, SceneItemStub? group) current = items[i];
            
            SceneItemViewModel? sceneItem = await CreateSceneItem(current.item, current.group);
            if (sceneItem == null) continue;
            
            createdSceneItems.Add(sceneItem);
        }

        await Dispatcher.InvokeAsync(delegate
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

    public async Task Refresh()
    {
        await SetSceneItemsAsync(SceneName!, SceneUuid, true);
    }
    public async Task RefreshItems() 
    {
        for (int i = 0; i < SceneItems.Count; i++)
        {
            await SceneItems[i].RefreshAsync();
        }
    }

    private async Task<SceneItemViewModel?> CreateSceneItem(SceneItemStub item, SceneItemStub? group = null)
    {
        if (item.SceneItemTransform == null) return null;

        _appCache.SceneItemConfigs.TryGetValue(item.SourceUuid ?? string.Empty, out SceneItemConfiguration? config);
        SetCustomInputKind(item, config);
        
        string inputKindText = item.ExtensionData?[nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
        if (!IsInputKindCorrect(inputKindText)) return null;
        
        InputKind inputKind = Enum.TryParse<InputKind>(inputKindText, out var kind) ? kind : InputKind.unsupported;
        if (inputKind == InputKind.unsupported) return null;

        SceneItemViewModel? sceneItem = inputKind switch
        {
            InputKind.tt_point_of_view => new PointOfView(SceneController, Dispatcher, Logger, Type),
            InputKind.browser_source => new BrowserItemViewModel(SceneController, Dispatcher, Logger),
            InputKind.text_gdiplus_v2 or InputKind.text_gdiplus_v3 => new TextItemViewModel(SceneController, Dispatcher, Logger),
            _ => null
        };

        if (sceneItem == null) return null;

        await sceneItem.InitializeAsync(this, SceneController.InEditMode, item, group);
        sceneItem.Transform.UpdateProportions(ProportionsRatio);

        SceneController.SetupBindings(sceneItem);
        SetupConfiguration(sceneItem, config);

        return sceneItem;
    }
    public async Task AddSceneItem(SceneItemStub item, SceneItemStub? group = null)
    {
        SceneItemViewModel? sceneItem = await CreateSceneItem(item, group);
        if (sceneItem == null) return;
        
        await Dispatcher.InvokeAsync(delegate
        {
            lock (_lock)
            {
                SceneItems.Add(sceneItem);
            }
        });
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

    public void CalculateProportionsRatio(float baseWidth)
    {
        BaseWidth = baseWidth;
    }

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

    private void SetCustomInputKind(SceneItemStub sceneItem, SceneItemConfiguration? configuration)
    {
        if (configuration == null) return;
        if (sceneItem.ExtensionData == null || string.IsNullOrEmpty(sceneItem.SourceUuid)) return;

        sceneItem.ExtensionData[nameof(ExtensionDataType.inputKind)] = JsonSerializer.SerializeToElement(configuration.InputKind.ToString());
    }
    private void SetupConfiguration(SceneItemViewModel sceneItem, SceneItemConfiguration? configuration)
    {
        if (string.IsNullOrEmpty(sceneItem.SourceUUID)) return;
        if (configuration == null) return;

        sceneItem.BindingPath = configuration.BindingPath ?? string.Empty;
        sceneItem.InputKind = configuration.InputKind;
    }
    
    private bool IsInputKindCorrect(string itemInputKind)
    {
        if (string.IsNullOrEmpty(itemInputKind) ||
            (!SceneController.InEditMode && !itemInputKind.Equals(nameof(InputKind.tt_point_of_view)))) return false;
        
        return true;
    }
    
    public void Clear()
    {
        SceneName = string.Empty;
        SceneUuid = string.Empty;
        
        ClearSceneItems();
    }
}