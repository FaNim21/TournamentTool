using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public class TextItemViewModel : SceneItemViewModel
{
    public override int ZIndex { get; protected set; } = 10;
    public override string BaseItemType => "Text";

    protected string Text { get; set; } = string.Empty;
    

    public TextItemViewModel(ISceneControllerViewModel controllerViewModel, IDispatcherService dispatcher, ILoggingService logger) : base(controllerViewModel, dispatcher, logger)
    {
        DefaultColor = Consts.TextSourceColor;
    }

    public override async Task ApplyBindingValueAsync(object? value)
    {
        Text = value?.ToString() ?? string.Empty;
        
        await UpdateAsync();
    }
    
    protected override async Task UpdateAsync()
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