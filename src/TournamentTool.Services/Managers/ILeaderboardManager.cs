using TournamentTool.Domain.Entities.Ranking;

namespace TournamentTool.Services.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;

    void EvaluateData(object? data, LeaderboardRuleType ruleType = LeaderboardRuleType.None);
}