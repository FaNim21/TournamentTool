using System.Diagnostics;
using TournamentTool.Models.Ranking;

namespace TournamentTool.Factories;

public abstract class LeaderboardEntryMilestoneFactory
{
    public static EntryMilestoneData Create(LeaderboardPlayerEvaluateData evaluateData, int Points)
    {
        return evaluateData switch
        {
            LeaderboardPacemanEvaluateData data => new EntryPacemanMilestoneData(data.MainSplit, data.PreviousSplit, Points, data.WorldID),
            LeaderboardRankedEvaluateData data => new EntryRankedMilestoneData(data.MainSplit, data.PreviousSplit, Points, data.Round), 
            _ => throw new UnreachableException()
        };
    }
    
    public static EntryMilestoneData DeepCopy(EntryMilestoneData milestoneData)
    {
        LeaderboardTimeline mainTimeline = new LeaderboardTimeline(milestoneData.Main.Milestone, milestoneData.Main.Time);
        LeaderboardTimeline? previousTimeline = null;
        if (milestoneData.Previous != null)
            previousTimeline = new LeaderboardTimeline(milestoneData.Previous.Milestone, milestoneData.Previous.Time);
        
        return milestoneData switch
        {
            EntryPacemanMilestoneData data => new EntryPacemanMilestoneData(mainTimeline, previousTimeline, data.Points, data.WorldID),
            EntryRankedMilestoneData data => new EntryRankedMilestoneData(mainTimeline, previousTimeline, data.Points, data.Round), 
            _ => throw new UnreachableException()
        };
    }
}