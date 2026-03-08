using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Obs;

public class TextItemViewModel : SceneItemViewModel
{
    public override int ZIndex { get; protected set; } = 10;

    protected string Text { get; set; } = string.Empty;

    public TextItemViewModel(ISceneController controller, IDispatcherService dispatcher, ILoggingService logger) : base(controller, dispatcher, logger)
    {
        BackgroundColor = Consts.TextSourceColor;
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