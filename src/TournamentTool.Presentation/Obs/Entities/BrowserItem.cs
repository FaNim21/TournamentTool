using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Presentation.Obs.Entities;

public class BrowserItem : SceneItem
{
    public override string BaseItemType => "Browser";
    
    public string Url { get; set; } = string.Empty;
    
    
    public BrowserItem(ISceneManager sceneManager, ILoggingService logger) 
        : base(sceneManager, logger) { }

    public override SceneItem Clone(IScene scene)
    {
        BrowserItem clonedItem = new BrowserItem(SceneManager, Logger);
        clonedItem.Initialize(scene, _item, _group, new SceneItemConfiguration(InputKind, BindingKey));
        return clonedItem;
    }
    
    public override void ApplyBindingValue(object? value)
    {
        Url = value?.ToString() ?? string.Empty;
        
        Update();
    }

    public override void Update()
    {
        Inputs["url"] = Url;
        
        base.Update();
    }

    public override void Clear(bool fullClear = false)
    {
        Url = string.Empty;
        Update();
        
        base.Clear(fullClear);
    }
}