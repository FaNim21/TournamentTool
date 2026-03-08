using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Obs;

public class BrowserItemViewModel : SceneItemViewModel
{
    public override int ZIndex { get; protected set; } = 10;

    protected string Url { get; set; } = string.Empty;

    
    public BrowserItemViewModel(ISceneController controller, IDispatcherService dispatcher, ILoggingService logger) : base(controller, dispatcher, logger)
    {
        BackgroundColor = Consts.BrowserSourceColor;
    }

    public override async Task UpdateAsync()
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