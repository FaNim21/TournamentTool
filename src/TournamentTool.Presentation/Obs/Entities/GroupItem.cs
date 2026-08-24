using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Presentation.Obs.Entities;

public class GroupItem : SceneItem
{
    public override string BaseItemType => "Group";
    
    public GroupItem(ISceneManager sceneManager, ILoggingService logger) 
        : base(sceneManager, logger) { }
    
    public override SceneItem Clone(Scene scene)
    {
        GroupItem clonedItem = new GroupItem(SceneManager, Logger);
        clonedItem.Initialize(scene, _item, _group, new SceneItemConfiguration(InputKind, BindingKey));
        return clonedItem;
    }
}