using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Obs;

public abstract class SceneItemViewModel : BaseViewModel
{
    //TODO: 0 Parent od relacji i mogą to być: SceneItem, Leaderboard (entries, rules), SidePanel, ManagementPanel
    //TODO: 0 lista dzieci, ktore sa aktualizowane przez rodzica, bo aktualizacji informacji

    protected ILoggingService Logger { get; }
    
    protected ISceneController Controller { get; }
    protected IScene Scene { get; private set; } = null!;

    public TransformViewModel Transform { get; init; }

    public virtual int ZIndex { get; protected set; }

    public bool IsDisplayed { get; protected set; } = true;
    public bool IsFocused { get; protected set; }

    public string SourceName { get; protected set; } = string.Empty;
    protected string SourceUUID { get; set; } = string.Empty;

    protected Dictionary<string, object> Inputs { get; } = [];

    public string GroupName { get; protected set; } = string.Empty;

    public string? BackgroundColor { get; set; }

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
        Scene = scene;
        SourceName = item.SourceName ?? string.Empty;
        SourceUUID = item.SourceUuid ?? string.Empty;
        GroupName = group != null ? group.SourceName ?? string.Empty : string.Empty;
        
        Transform.Initialize(item.SceneItemTransform!, group?.SceneItemTransform);
        
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
        BackgroundColor = Consts.UnFocusedPovColor;
        OnPropertyChanged(nameof(BackgroundColor));
        
        ZIndex = _rememberedZIndex;
        OnPropertyChanged(nameof(ZIndex));
    }

    public virtual Task ClearAsync(bool fullClear = false) => Task.CompletedTask;
}