using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public abstract class TextItemViewModel<T> : SceneItemViewModel<T> where T : TextItem
{
    public override int ZIndex { get; protected set; } = 10;

    public string Text
    {
        get => _sceneItem.Text;
        set
        {
            _sceneItem.Text = value;
            OnPropertyChanged();
        }
    }


    protected TextItemViewModel(T sceneItem, IDispatcherService dispatcher, ILoggingService logger) 
        : base(sceneItem, dispatcher, logger) { }
}

public class TextItemViewModel : TextItemViewModel<TextItem>
{
    public TextItemViewModel(TextItem sceneItem, IDispatcherService dispatcher, ILoggingService logger) : base(sceneItem, dispatcher, logger)
    {
        DefaultColor = Consts.TextSourceColor;
    }
}