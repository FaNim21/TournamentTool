using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Presentation.Obs.Entities;

public abstract class SceneItem : IBindingTarget 
{
    protected ILoggingService Logger { get; }
    
    protected ISceneManager SceneManager { get; }
    protected IScene Scene { get; private set; } = null!;

    public Transform Transform { get; init; } = new();

    public BindingKey BindingKey { get; private set; } = BindingKey.Empty();

    public abstract string BaseItemType { get; }
    public InputKind InputKind { get; protected set; }

    public string GroupName { get; protected set; } = string.Empty;
    public string SourceName { get; protected set; } = string.Empty;
    public string SourceUUID { get; private set; } = string.Empty;

    protected Dictionary<string, object> Inputs { get; } = [];

    
    protected SceneItem(ISceneManager sceneManager, ILoggingService logger)
    {
        SceneManager = sceneManager;
        Logger = logger;
    }
    public virtual void OnDestroy()
    {
        if (Scene.IsReadonly) return;
        
        SceneManager.UnregisterTarget(BindingKey, this);
    }
    
    public void UpdateBinding(BindingKey newKey)
    {
        if (Scene.IsReadonly) return;
        
        SceneManager.UnregisterTarget(BindingKey, this);
        
        BindingKey = newKey;
        SceneManager.RegisterTarget(newKey, this);
    }

    public virtual void Initialize(IScene scene, SceneItemStub item, SceneItemStub? group = null, SceneItemConfiguration? configuration = null)
    {
        Scene = scene;
        SourceName = item.SourceName ?? string.Empty;
        SourceUUID = item.SourceUuid ?? string.Empty;
        GroupName = group != null ? group.SourceName ?? string.Empty : string.Empty;
        
        Transform.Initialize(item.SceneItemTransform!, group?.SceneItemTransform);
        
        string inputKindText = item.ExtensionData?[nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(inputKindText))
        {
            InputKind inputKind = Enum.TryParse<InputKind>(inputKindText, out var kind) ? kind : InputKind.unsupported;
            InputKind = inputKind;
        }
        
        SetupConfiguration(configuration);
        
        if (Scene.IsReadonly) return;
        SceneManager.RegisterTarget(BindingKey, this);
    }

    public virtual Task LoadAsync() => Task.CompletedTask;
    public virtual void Update() => SceneManager.QueueUpdate(SourceUUID, Inputs);
    protected async Task ForceUpdateAsync() => await SceneManager.SetItemInputSettingsAsync(SourceUUID, Inputs);

    public virtual Task RefreshAsync() => Task.CompletedTask;

    public virtual void ApplyBindingValue(object? value) { }
    public virtual void Clear(bool fullClear = false) { }
    
    public void SetupConfiguration(SceneItemConfiguration? configuration)
    {
        if (string.IsNullOrEmpty(SourceUUID)) return;
        if (configuration == null) return;

        BindingKey = configuration.BindingKey;
        InputKind = configuration.InputKind;
    }
}