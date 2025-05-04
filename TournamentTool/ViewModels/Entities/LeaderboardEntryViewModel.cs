using TournamentTool.Models;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardEntryViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _entry;

    public PlayerViewModel? Player { get; set; }


    
    public LeaderboardEntryViewModel(LeaderboardEntry entry, PlayerViewModel? player)
    {
        _entry = entry;
        Player = player;
    }

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}