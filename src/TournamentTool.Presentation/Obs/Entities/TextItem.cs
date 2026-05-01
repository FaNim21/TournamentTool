using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.Presentation.Obs.Entities;

public class TextItem : SceneItem
{
    public override string BaseItemType => "Text";
    
    public string Text { get; set; } = string.Empty;
    
    public TextItem(ISceneManager sceneManager, ILoggingService logger) : 
        base(sceneManager, logger) { }
    
    public override void ApplyBindingValue(object? value)
    {
        Text = value?.ToString() ?? string.Empty;
        
        Update();
    }
    
    public override void Update()
    {
        Inputs["text"] = Text;
        
        base.Update();
    }

    public override void Clear(bool fullClear = false)
    {
        Text = string.Empty;
        Update();
        
        base.Clear(fullClear);
    }
}