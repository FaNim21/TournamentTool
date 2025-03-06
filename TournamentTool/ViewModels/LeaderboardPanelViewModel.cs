using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class LeaderboardPanelViewModel : SelectableViewModel
{
    public TournamentViewModel Tournament { get; }

    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament) : base(coordinator)
    {
        Tournament = tournament;
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    }

    public override void OnEnable(object? parameter)
    {
        
    }
    public override bool OnDisable()
    {
        return true;
    }
}