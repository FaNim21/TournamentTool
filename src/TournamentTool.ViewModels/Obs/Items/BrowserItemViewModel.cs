using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public class BrowserItemViewModel : SceneItemViewModel
{
    public override int ZIndex { get; protected set; } = 10;
    public override string BaseItemType => "Browser";

    protected string Url { get; set; } = string.Empty;

    
    public BrowserItemViewModel(ISceneController controller, IDispatcherService dispatcher, ILoggingService logger) : base(controller, dispatcher, logger)
    {
        DefaultColor = Consts.BrowserSourceColor;
    }

    public override async Task ApplyBindingValueAsync(object? value)
    {
        Url = value?.ToString() ?? string.Empty;
        
        await UpdateAsync();
    }

    protected override async Task UpdateAsync()
    {
        Inputs["url"] = Url;
        
        await base.UpdateAsync();
    }

    public override async Task ClearAsync(bool fullClear = false)
    {
        Url = string.Empty;
        await UpdateAsync();
        
        await base.ClearAsync(fullClear);
    }
}