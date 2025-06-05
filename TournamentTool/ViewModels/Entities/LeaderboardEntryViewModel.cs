using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardEntryViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _entry;

    public PlayerViewModel? Player { get; }

    public string PlayerUUID => _entry.PlayerUUID;
    public int Points
    {
        get => _entry.Points;
        set
        {
            _entry.Points = value;
            OnPropertyChanged(nameof(Points));
        }
    }
    public int Position
    {
        get => _entry.Position;
        set
        {
            _entry.Position = value;
            OnPropertyChanged(nameof(Position));
        }
    }

    //zrobic bool'a do wykrywania czy gracz zostal dodany w kontekscie usuniecia gracza z whitelisty, ktory byl w leaderboardzie
 

    public LeaderboardEntryViewModel(LeaderboardEntry entry, PlayerViewModel? player)
    {
        _entry = entry;
        Player = player;
        Player?.UpdateHeadBitmap();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Points));
        OnPropertyChanged(nameof(Position));
    }

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}