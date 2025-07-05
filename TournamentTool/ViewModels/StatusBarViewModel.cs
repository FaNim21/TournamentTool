using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class StatusBarViewModel : BaseViewModel
{
    public TournamentViewModel Tournament { get; }
    
    

    public StatusBarViewModel(TournamentViewModel tournament)
    {
        Tournament = tournament;
    }
}