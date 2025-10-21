using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;

namespace TournamentTool.ViewModels.Selectable;

public class SceneManagementViewModel : SelectableViewModel
{
    //
    
    public SceneManagementViewModel(ICoordinator coordinator, IDispatcherService dispatcher) : base(coordinator, dispatcher)
    {
    }
}