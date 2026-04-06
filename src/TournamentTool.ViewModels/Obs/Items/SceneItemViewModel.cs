using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public abstract class SceneItemViewModel<T> : SceneItemViewModel where T : SceneItem
{
    protected readonly T _sceneItem;
    public override SceneItem SceneItem => _sceneItem;

    protected SceneItemViewModel(T sceneItem, IDispatcherService dispatcher, ILoggingService logger) 
        : base(dispatcher, logger)
    {
        _sceneItem = sceneItem;
        
        Transform = new TransformViewModel(SceneItem.Transform, dispatcher);
    }
}

public abstract class SceneItemViewModel : BaseViewModel
{
    public abstract SceneItem SceneItem { get; }
    protected ILoggingService Logger { get; }

    public TransformViewModel Transform { get; protected init; } = new(null!, null!);

    public bool IsDisplayed { get; protected set; } = true;
    public float Opacity { get; protected set; } = 1f;
    public virtual int ZIndex { get; protected set; }
    public InputKind InputKind => SceneItem.InputKind;

    public bool InEditMode { get; protected set; }
    public bool IsFocused { get; protected set; }

    public string GroupName => SceneItem.GroupName;
    
    public string SourceName => SceneItem.SourceName;
    public string SourceUUID => SceneItem.SourceUUID;

    public string? BackgroundColor { get; set; }
    protected string? DefaultColor { get; init; }

    private int _rememberedZIndex;

    
    protected SceneItemViewModel(IDispatcherService dispatcher, ILoggingService logger) : base(dispatcher)
    {
        Logger = logger;

        _rememberedZIndex = ZIndex;
        UnFocus();
    }
    public virtual void OnDestroy() { }

    public virtual void Initialize(bool inEditMode, bool isDisplayed)
    {
        BackgroundColor = DefaultColor;
        
        Opacity = inEditMode ? 0.5f : 1f;
        IsDisplayed = isDisplayed;
        InEditMode = inEditMode;
    }
    
    protected async Task UpdateAsync() => await SceneItem.UpdateAsync();

    public virtual async Task RefreshAsync() => await SceneItem.RefreshAsync();
    public virtual async Task ClearAsync(bool fullClear = false) => await SceneItem.ClearAsync(fullClear);

    public void Focus()
    {
        IsFocused = true;
        BackgroundColor = Consts.FocusedPovColor;
        OnPropertyChanged(nameof(BackgroundColor));

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
}