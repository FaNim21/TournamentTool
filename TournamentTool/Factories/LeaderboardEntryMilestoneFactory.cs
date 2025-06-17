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
            LeaderboardRankedEvaluateData data => new EntryRankedMilestoneData(data.MainSplit, data.PreviousSplit, Points), 
            _ => throw new UnreachableException()
        };
    }
}