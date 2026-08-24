using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Obs.Items;

public class GroupItemViewModel<T> : SceneItemViewModel<T> where T : GroupItem
{
    public override int ZIndex { get; protected set; } = -1;

    
    protected GroupItemViewModel(T sceneItem, IDispatcherService dispatcher, ILoggingService logger)
        : base(sceneItem, dispatcher, logger) { }
}

public class GroupItemViewModel : GroupItemViewModel<GroupItem>
{
    public GroupItemViewModel(GroupItem sceneItem, IDispatcherService dispatcher, ILoggingService logger) : base(sceneItem, dispatcher, logger)
    {
        DefaultColor = Consts.GroupSourceColor;
    }
}