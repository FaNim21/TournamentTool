using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

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

    private string _bestTimeOnPrioritizeMilestone = string.Empty;
    public string BestTimeOnPrioritizeMilestone
    {
        get => _bestTimeOnPrioritizeMilestone;
        set
        {
            _bestTimeOnPrioritizeMilestone = value;
            OnPropertyChanged(nameof(BestTimeOnPrioritizeMilestone));
        }
    }

    private string _averageTimeOnPrioritizeMilestone = string.Empty;
    public string AverageTimeOnPrioritizeMilestone
    {
        get => _averageTimeOnPrioritizeMilestone;
        set
        {
            _averageTimeOnPrioritizeMilestone = value;
            OnPropertyChanged(nameof(AverageTimeOnPrioritizeMilestone));
        }
    }

    //zrobic bool'a do wykrywania czy gracz zostal dodany w kontekscie usuniecia gracza z whitelisty, ktory byl w leaderboardzie
    // Console.WriteLine($"Run url: https://paceman.gg/stats/run/{paceman.WorldID}");
 

    public LeaderboardEntryViewModel(LeaderboardEntry entry, PlayerViewModel? player)
    {
        _entry = entry;
        Player = player;
        Player?.UpdateHeadBitmap();
    }

    public void Refresh(RunMilestone milestone = RunMilestone.None)
    {
        if (milestone != RunMilestone.None)
        {
            RefreshBestMilestone(milestone);
        }
            
        OnPropertyChanged(nameof(Points));
        OnPropertyChanged(nameof(Position));
    }
    public void RefreshBestMilestone(RunMilestone milestone)
    {
        var data = _entry.GetBestMilestone(milestone);
        if (data == null)
        {
            BestTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(0).ToFormattedTime();
            AverageTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(0).ToFormattedTime(); 
            return;
        }
        
        BestTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(data.BestTime).ToFormattedTime();
        AverageTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(data.Average).ToFormattedTime(); 
    }

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}