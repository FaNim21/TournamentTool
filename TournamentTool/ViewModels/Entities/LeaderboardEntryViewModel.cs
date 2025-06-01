using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardEntryViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _entry;

    public PlayerViewModel? Player { get; set; }

    public int Points
    {
        get => _entry.Points;
        set
        {
            _entry.Points = value;
            OnPropertyChanged(nameof(Points));
        }
    }


    public LeaderboardEntryViewModel(LeaderboardEntry entry, PlayerViewModel? player)
    {
        _entry = entry;
        Player = player;
    }

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}