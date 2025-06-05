using TournamentTool.Models.Ranking;

namespace TournamentTool.Factories;

public abstract class LeaderboardEntryMilestoneFactory
{
    public static EntryMilestoneData? Create(LeaderboardPlayerEvaluateData evaluateData, int Points)
    {
        return evaluateData switch
        {
            LeaderboardPacemanEvaluateData data => new EntryPacemanMilestoneData(data.MainSplit, data.MainSplit, Points, data.WorldID),
            LeaderboardRankedEvaluateData data => new EntryRankedMilestoneData(data.MainSplit, data.MainSplit, Points), 
            _ => null
        };
    }
}