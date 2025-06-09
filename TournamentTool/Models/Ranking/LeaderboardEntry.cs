using System.Text.Json.Serialization;
using TournamentTool.Enums;
using TournamentTool.Utils;

namespace TournamentTool.Models.Ranking;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(EntryPacemanMilestoneData), "paceman")]
[JsonDerivedType(typeof(EntryRankedMilestoneData), "ranked")]
public abstract record EntryMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline Previous, int Points);
public record EntryPacemanMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline Previous, int Points, string WorldID) : EntryMilestoneData(Main, Previous, Points);
public record EntryRankedMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline Previous, int Points) : EntryMilestoneData(Main, Previous, Points);

public class BestMilestoneData
{
    public int BestTime { get; set; } = int.MaxValue;
    public int AllTimes { get; set; }
    public int Amount { get; set; }
    [JsonIgnore] public int Average => AllTimes / Amount;
    
    public void AddTime(LeaderboardTimeline data)
    {
        AllTimes += data.Time;
        Amount++;

        if (data.Time < BestTime)
        {
            BestTime = data.Time;
        }
    }
}

public sealed class LeaderboardEntry
{
    public string PlayerUUID { get; init; } = string.Empty;

    public int Points { get; set; }
    public int Position { get; set; } = -1;

    public Dictionary<RunMilestone, BestMilestoneData> BestMilestones { get; init; } = [];
    public List<EntryMilestoneData> Milestones { get; init; } = [];


    public void AddPoints(int points)
    {
        Points += points;
    }
    public void AddMilestone(EntryMilestoneData data)
    {
        BestMilestones.TryGetValue(data.Main.Milestone, out var bestMilestone);
        if (bestMilestone == null)
        {
            bestMilestone = new BestMilestoneData();
            BestMilestones[data.Main.Milestone] = bestMilestone;
        }
        
        //temp pod debugowanie
        if (data.Main.Time < bestMilestone.BestTime)
        {
            string oldTime = "Unknown";
            if (bestMilestone.BestTime != int.MaxValue)
                oldTime = TimeSpan.FromMilliseconds(bestMilestone.BestTime).ToFormattedTime();
            string newTime = TimeSpan.FromMilliseconds(data.Main.Time).ToFormattedTime();
            
            Console.WriteLine($"Player {PlayerUUID} beats his best time on {data.Main.Milestone} from {oldTime} to {newTime}");
        }
        bestMilestone.AddTime(data.Main);
        
        Milestones.Add(data);
        AddPoints(data.Points);
    }

    public BestMilestoneData? GetBestMilestone(RunMilestone milestone)
    {
        BestMilestones.TryGetValue(milestone, out var data);
        return data;
    }
    public int GetBestMilestoneTime(RunMilestone milestone)
    {
        var data = GetBestMilestone(milestone);
        int time = int.MaxValue;
        if (data != null) time = data.BestTime;
        return time;
    }
    
    public int CompareTo(LeaderboardEntry other, RunMilestone milestone)
    {
        int pointComparison = Points.CompareTo(other.Points);
        if (pointComparison != 0) return pointComparison;

        int bestMilestoneComparison = GetBestMilestoneTime(milestone).CompareTo(other.GetBestMilestoneTime(milestone));
        if (bestMilestoneComparison != 0) return bestMilestoneComparison * -1;

        return 0;
    }
}