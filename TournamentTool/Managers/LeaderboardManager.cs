using TournamentTool.Factories;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;
    bool IsLeaderboardWorking { get; set; } // to jest tymczasowo do testow

    void EvaluatePlayer(LeaderboardPlayerEvaluateData data);
}

public class LeaderboardManager : ILeaderboardManager
{
    private TournamentViewModel Tournament { get; }

    public event Action<LeaderboardEntry>? OnEntryUpdate;

    public bool IsLeaderboardWorking { get; set; } = true;

    
    public LeaderboardManager(TournamentViewModel tournament)
    {
        Tournament = tournament;
    }
    
    public void EvaluatePlayer(LeaderboardPlayerEvaluateData data)
    {
        if (!IsLeaderboardWorking) return;
        if (data.Player == null) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;
        
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
        var playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
        var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Console.WriteLine($"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" in time: {playerTime}, so under {subRuleTime} with new points: {subRule.BasePoints}");

        LeaderboardEntry entry = Tournament.Leaderboard.GetOrCreateEntry(data.Player.UUID);
        var milestone = LeaderboardEntryMilestoneFactory.Create(data, subRule.BasePoints);
        if (milestone == null) return;
        entry.AddMilestone(milestone);
        
        Tournament.Leaderboard.RecalculateEntryPosition(entry);
        OnEntryUpdate?.Invoke(entry);
    }
}