using TournamentTool.Modules;

namespace TournamentTool.ViewModels;

public class LeaderboardViewModel : SelectableViewModel
{
    
    
    public LeaderboardViewModel(MainViewModelCoordinator coordinator) : base(coordinator)
    {
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
    }

    public override bool OnDisable()
    {
        return true;
    }
}