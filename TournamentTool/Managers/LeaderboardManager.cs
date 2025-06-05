using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;
    
    void EvaluatePlayer(LeaderboardPlayerEvaluateData data);
}

public class LeaderboardManager : ILeaderboardManager
{
    private TournamentViewModel Tournament { get; }

    public event Action<LeaderboardEntry>? OnEntryUpdate;
    
    
    public LeaderboardManager(TournamentViewModel tournament)
    {
        Tournament = tournament;
    }
    
    public void EvaluatePlayer(LeaderboardPlayerEvaluateData data)
    {
        if (data.Player == null) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;
        
        /*
        Console.WriteLine(data.Player == null
            ? $"Player: ??? achieved milestone -> checking all rules ({data.MainSplit.Milestone})"
            : $"Player: \"{data.Player.InGameName}\" achieved milestone -> checking all rules ({data.MainSplit.Milestone})");
            */

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
            // Console.WriteLine($"Run url: https://paceman.gg/stats/run/{paceman.WorldID}");
        }
        
        var playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
        var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Console.WriteLine($"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" in time: {playerTime}, so under {subRuleTime} with new points: {subRule.BasePoints}");

        LeaderboardEntry entry = Tournament.Leaderboard.GetOrCreateEntry(data.Player.UUID);
        entry.AddPoints(subRule.BasePoints);
        Tournament.Leaderboard.RecalculateEntryPosition(entry);
        OnEntryUpdate?.Invoke(entry);
    }
}