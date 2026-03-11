using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public abstract class SceneItemViewModel : BaseViewModel
{
    //TODO: 0 Parent od relacji i mogą to być: SceneItem, Leaderboard (entries, rules), SidePanel, ManagementPanel
    //TODO: 0 lista dzieci, ktore sa aktualizowane przez rodzica, bo aktualizacji informacji

    protected ILoggingService Logger { get; }
    
    protected ISceneController Controller { get; }
    protected IScene Scene { get; private set; } = null!;

    public TransformViewModel Transform { get; init; }

    public virtual int ZIndex { get; protected set; }
    public abstract string BaseItemType { get; }
    public InputKind InputKind { get; protected set; }

    public bool IsDisplayed { get; protected set; } = true;
    public bool IsFocused { get; protected set; }

    public string GroupName { get; protected set; } = string.Empty;
    
    public string SourceName { get; protected set; } = string.Empty;
    public string SourceUUID { get; private set; } = string.Empty;

    protected Dictionary<string, object> Inputs { get; } = [];

    public string? BackgroundColor { get; set; }
    protected string? DefaultColor { get; init; }

    private int _rememberedZIndex;

    
    protected SceneItemViewModel(ISceneController controller, IDispatcherService dispatcher, ILoggingService logger) : base(dispatcher)
    {
        Controller = controller;
        Logger = logger;
        Transform = new TransformViewModel(dispatcher);

        _rememberedZIndex = ZIndex;
        UnFocus();
    }
    public virtual void OnDestroy() { }

    public virtual Task InitializeAsync(IScene scene, SceneItemStub item, SceneItemStub? group = null)
    {
        BackgroundColor = DefaultColor;
        
        Scene = scene;
        SourceName = item.SourceName ?? string.Empty;
        SourceUUID = item.SourceUuid ?? string.Empty;
        GroupName = group != null ? group.SourceName ?? string.Empty : string.Empty;
        
        Transform.Initialize(item.SceneItemTransform!, group?.SceneItemTransform);
        
        string inputKindText = item.ExtensionData?[nameof(ExtensionDataType.inputKind)].ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(inputKindText)) return Task.CompletedTask;
        
        InputKind inputKind = Enum.TryParse<InputKind>(inputKindText, out var kind) ? kind : InputKind.unsupported;
        InputKind = inputKind;

        return Task.CompletedTask;
    }

    public virtual async Task UpdateAsync() => await Controller.SetItemInputSettingsAsync(SourceUUID, Inputs);

    public virtual Task RefreshAsync() => Task.CompletedTask;

    public void Focus()
    {
        IsFocused = true;
        BackgroundColor = Consts.FocusedPovColor;
        OnPropertyChanged(nameof(BackgroundColor));

        //TODO: 0 ZIndex chyba nie dziala
        _rememberedZIndex = ZIndex;
        ZIndex = 9999;
        OnPropertyChanged(nameof(ZIndex));
    }
    public void UnFocus()
    {
        IsFocused = false;
        BackgroundColor = DefaultColor ?? Consts.UnFocusedPovColor;
        OnPropertyChanged(nameof(BackgroundColor));
        
        ZIndex = _rememberedZIndex;
        OnPropertyChanged(nameof(ZIndex));
    }

    public virtual Task ClearAsync(bool fullClear = false) => Task.CompletedTask;
}