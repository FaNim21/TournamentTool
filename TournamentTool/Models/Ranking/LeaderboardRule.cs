namespace TournamentTool.Models.Ranking;

public enum LeaderboardRuleType
{
    Split,
    Advancement
}

public class LeaderboardRule
{
    public string Name { get; set; } = string.Empty;
    public LeaderboardRuleType Type { get; set; } = LeaderboardRuleType.Split;
    public List<LeaderboardSubRule> SubRules { get; set; } = [];
}