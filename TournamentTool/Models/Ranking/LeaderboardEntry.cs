using System.Text.Json.Serialization;
using TournamentTool.Enums;
using TournamentTool.Utils;

namespace TournamentTool.Models.Ranking;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(EntryPacemanMilestoneData), "paceman")]
[JsonDerivedType(typeof(EntryRankedMilestoneData), "ranked")]
public abstract record EntryMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline? Previous, int Points);
public record EntryPacemanMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline? Previous, int Points, string WorldID) : EntryMilestoneData(Main, Previous, Points);
public record EntryRankedMilestoneData(LeaderboardTimeline Main, LeaderboardTimeline? Previous, int Points) : EntryMilestoneData(Main, Previous, Points);

public class BestMilestoneData
{
    public int BestTime { get; set; } = int.MaxValue;
    public long AllTimes { get; set; }
    public int Amount { get; set; }
    [JsonIgnore] public int Average => (int)(AllTimes / Amount);
    
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
    public bool AddMilestone(EntryMilestoneData data)
    {
        if (Milestones.Count > 0 && data == Milestones[^1]) return false;
        
        BestMilestones.TryGetValue(data.Main.Milestone, out var bestMilestone);
        if (bestMilestone == null)
        {
            bestMilestone = new BestMilestoneData();
            BestMilestones[data.Main.Milestone] = bestMilestone;
        }
        
        bestMilestone.AddTime(data.Main);
        Milestones.Add(data);
        AddPoints(data.Points);
        return true;
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