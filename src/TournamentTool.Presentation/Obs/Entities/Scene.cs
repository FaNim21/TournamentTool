using System.Text.Json;
using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;

namespace TournamentTool.Presentation.Obs.Entities;

public enum SceneType
{
    Main,
    Preview
}

public interface IScene
{
    string SceneName { get; }
    string SceneUuid { get; }

    bool IsReadonly { get; }

    bool ExistInItems<T>(Func<T, bool> condition) where T : SceneItem;
    T? GetItem<T>(Func<T, bool> condition) where T : SceneItem;
}

public class Scene : IScene
{
    private readonly ISceneManager _sceneManager;
    private readonly ILoggingService _logger;
    private readonly AppCache _appCache;
    private readonly Lock _lock = new();
    
    protected SceneType Type { get; set; }

    public List<SceneItem> SceneItems { get; set; } = [];

    public string SceneName { get; private set; } = string.Empty;
    public string SceneUuid { get; private set; } = string.Empty;

    public float BaseWidth { get; set; }

    public bool IsReadonly { get; init; }

    public event EventHandler<SceneItem>? ItemAdded;
    public event EventHandler<SceneItem>? ItemRemoved;
    public event EventHandler? ItemsCleared;
    public event EventHandler<Scene>? SceneRecreated;


    public Scene(ISceneManager sceneManager, ILoggingService logger, AppCache appCache, SceneType type)
    {
        _sceneManager = sceneManager;
        _logger = logger;
        _appCache = appCache;

        Type = type;
    }
    
    public Scene Clone()
    {
        Scene clonedScene = new(_sceneManager, _logger, _appCache, Type)
        {
            SceneItems = [..SceneItems],
            SceneName = SceneName,
            SceneUuid = SceneUuid,
            BaseWidth = BaseWidth,
            IsReadonly = true
        };

        return clonedScene;
    }
    
    public void Swap(Scene other)
    {
        List<SceneItem> povs = other.SceneItems;
        float baseWidth = other.BaseWidth;
        string sceneName = other.SceneName;
        string sceneUuid = other.SceneUuid;

        other.SceneName = SceneName;
        other.SceneUuid = SceneUuid;
        other.SceneItems = SceneItems;
        other.BaseWidth = BaseWidth;

        SceneName = sceneName;
        SceneUuid = sceneUuid;
        SceneItems = povs;
        BaseWidth = baseWidth;
        
        SceneRecreated?.Invoke(null, this);
        other.SceneRecreated?.Invoke(null, other);
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
        
        List<(SceneItemStub, SceneItemStub?)> items = await _sceneManager.GetSceneItemsAsync(sceneName, sceneUuid);
        List<SceneItem> createdSceneItems = [];
        
        for (int i = items.Count - 1; i >= 0; i--)
        {
            (SceneItemStub item, SceneItemStub? group) current = items[i];
            
            SceneItem? sceneItem = CreateSceneItem(current.item, current.group);
            if (sceneItem == null) continue;
            
            createdSceneItems.Add(sceneItem);
        }
        
        foreach (var item in createdSceneItems)
        {
            await item.LoadAsync();
        }
        
        lock (_lock)
        {
            SceneItems.Clear();
            ItemsCleared?.Invoke(this, EventArgs.Empty);
            
            foreach (var item in createdSceneItems)
            {
                SceneItems.Add(item);
                ItemAdded?.Invoke(this, item);
            }
        }
    }

    public async Task RefreshAsync() => await SetSceneItemsAsync(SceneName, SceneUuid, true);
    public async Task RefreshItems() 
    {
        for (int i = 0; i < SceneItems.Count; i++)
        {
            await SceneItems[i].RefreshAsync();
        }
    }

    private SceneItem? CreateSceneItem(SceneItemStub item, SceneItemStub? group = null)
    {
        if (item.SceneItemTransform == null) return null;

        _appCache.SceneItemConfigs.TryGetValue(item.SourceUuid ?? string.Empty, out SceneItemConfiguration? config);
        SetCustomInputKind(item, config);
        
        string inputKindText = item.ExtensionData?[nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(inputKindText)) return null;
        
        InputKind inputKind = Enum.TryParse<InputKind>(inputKindText, out var kind) ? kind : InputKind.unsupported;
        if (inputKind == InputKind.unsupported) return null;

        SceneItem? sceneItem = inputKind switch
        {
            InputKind.tt_point_of_view => new PointOfView(_sceneManager, _logger, Type),
            InputKind.browser_source => new BrowserItem(_sceneManager, _logger),
            InputKind.text_gdiplus_v2 or InputKind.text_gdiplus_v3 => new TextItem(_sceneManager, _logger),
            _ => null
        };

        if (sceneItem == null) return null;

        sceneItem.Initialize(this, item, group, config);

        return sceneItem;
    }
    public async Task AddSceneItem(SceneItemStub item, SceneItemStub? group = null)
    {
        SceneItem? sceneItem = CreateSceneItem(item, group);
        if (sceneItem == null) return;

        await sceneItem.LoadAsync();
        
        lock (_lock)
        {
            SceneItems.Add(sceneItem);
            ItemAdded?.Invoke(this, sceneItem);
        }
    }
    
    public void RemoveSceneItem(SceneItem item)
    {
        lock (_lock)
        {
            item.OnDestroy();
            SceneItems.Remove(item);
            ItemRemoved?.Invoke(this, item);
        }
    }
    public void ClearSceneItems()
    {
        foreach (SceneItem item in SceneItems)
        {
            item.OnDestroy();
        }

        lock (_lock)
        {
            SceneItems.Clear();
        }
        
        ItemsCleared?.Invoke(this, EventArgs.Empty);
    }

    public void SetBaseWidth(float baseWidth) => BaseWidth = baseWidth;

    public bool ExistInItems<T>(Func<T, bool> condition) where T : SceneItem => SceneItems.OfType<T>().Any(condition);
    public T? GetItem<T>(Func<T, bool> condition) where T : SceneItem => SceneItems.OfType<T>().FirstOrDefault(condition);

    private void SetCustomInputKind(SceneItemStub sceneItem, SceneItemConfiguration? configuration)
    {
        if (configuration == null) return;
        if (sceneItem.ExtensionData == null || string.IsNullOrEmpty(sceneItem.SourceUuid)) return;

        sceneItem.ExtensionData[nameof(ExtensionDataType.inputKind)] = JsonSerializer.SerializeToElement(configuration.InputKind.ToString());
    }
    
    public void Clear()
    {
        SceneName = string.Empty;
        SceneUuid = string.Empty;
        
        ClearSceneItems();
    }
}