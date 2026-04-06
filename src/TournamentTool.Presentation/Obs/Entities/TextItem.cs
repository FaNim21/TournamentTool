using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.Presentation.Obs.Entities;

public class TextItem : SceneItem
{
    public override string BaseItemType => "Text";
    
    public string Text { get; set; } = string.Empty;
    
    public TextItem(ISceneManager sceneManager, ILoggingService logger) : 
        base(sceneManager, logger) { }
    
    public override async Task ApplyBindingValueAsync(object? value)
    {
        Text = value?.ToString() ?? string.Empty;
        
        await UpdateAsync();
    }
    
    public override async Task UpdateAsync()
    {
        Inputs["text"] = Text;
        
        await base.UpdateAsync();
    }

    public override async Task ClearAsync(bool fullClear = false)
    {
        Text = string.Empty;
        await UpdateAsync();
        
        await base.ClearAsync(fullClear);
    }
}