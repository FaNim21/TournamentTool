using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public abstract class BrowserItemViewModel<T> : SceneItemViewModel<T> where T : BrowserItem
{
    public override int ZIndex { get; protected set; } = 10;
    
    public string Url
    {
        get => _sceneItem.Url;
        set
        {
            _sceneItem.Url = value;  
            OnPropertyChanged();
        } 
    }

    
    protected BrowserItemViewModel(T sceneItem, IDispatcherService dispatcher, ILoggingService logger) 
        : base(sceneItem, dispatcher, logger) { }
}

public class BrowserItemViewModel : BrowserItemViewModel<BrowserItem>
{
    public BrowserItemViewModel(BrowserItem sceneItem, IDispatcherService dispatcher, ILoggingService logger) : base(sceneItem, dispatcher, logger)
    {
        DefaultColor = Consts.BrowserSourceColor;
    }
}