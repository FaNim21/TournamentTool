using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Managers;

public interface ILeaderboardManager
{
    void EvaluatePlayer(LeaderboardPlayerEvaluateData data);
}

public class LeaderboardManager : ILeaderboardManager
{
    private TournamentViewModel Tournament { get; }

    
    public LeaderboardManager(TournamentViewModel tournament)
    {
        Tournament = tournament;
    }
    
    public void EvaluatePlayer(LeaderboardPlayerEvaluateData data)
    {
        if (data.Player == null) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;
        
        Console.WriteLine(data.Player == null
            ? $"Player: ??? achieved milestone -> checking all rules ({data.MainSplit.Milestone})"
            : $"Player: \"{data.Player.InGameName}\" achieved milestone -> checking all rules ({data.MainSplit.Milestone})");

        foreach (var rule in Tournament.Leaderboard.Rules)
        {
            var subRule = rule.Evaluate(data);
            if (subRule == null) continue;
            
            UpdateEntry(subRule, data);
            break;
        }
    }

    private void UpdateEntry(LeaderboardSubRule subRule, LeaderboardPlayerEvaluateData data)
    {
        if (data is LeaderboardPacemanEvaluateData paceman)
        {
            Console.WriteLine($"Run url: https://paceman.gg/stats/run/{paceman.WorldID}");
        }
        
        Console.WriteLine($"Updating entry for: {data.Player.InGameName} with new points: {subRule.BasePoints}");

        LeaderboardEntry entry = GetOrCreateEntry(data.Player.UUID);
        entry.AddPoints(subRule.BasePoints);
    }

    private LeaderboardEntry GetOrCreateEntry(string uuid)
    {
        LeaderboardEntry? entry = Tournament.Leaderboard.GetEntry(uuid);
        if (entry != null) return entry;
        
        entry = new LeaderboardEntry { PlayerUUID = uuid };
        Tournament.Leaderboard.Entries.Add(entry);
        return entry;
    }
}