using TournamentTool.Models;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardEntryViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _entry;

    public Player Player { get; set; }


    
    public LeaderboardEntryViewModel(LeaderboardEntry entry, Player player)
    {
        _entry = entry;
        Player = player;
    }

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}