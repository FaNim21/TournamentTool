using TournamentTool.Enums;
using TournamentTool.Utils;

namespace TournamentTool.Models.Ranking;

public enum LeaderboardRuleType
{
    None,
    Split,
    Advancement,
    All,
}

public class LeaderboardRule
{
    public string Name { get; set; } = string.Empty;
    public LeaderboardRuleType RuleType { get; set; } = LeaderboardRuleType.Split;
    public RunMilestone ChosenAdvancement { get; set; } = RunMilestone.None;
    public List<LeaderboardSubRule> SubRules { get; set; } = [];
    
    
    public LeaderboardSubRule? Evaluate(LeaderboardPlayerEvaluateData data)
    {
        foreach (var subRule in SubRules)
        {
            if (!subRule.Evaluate(data)) continue;
            
            var playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
            var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
            Console.WriteLine(data.Player == null
                ? $"Player: ??? just achieved milestone: \"{data.MainSplit.Milestone}\" in time: {playerTime}, so under {subRuleTime}"
                : $"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" in time: {playerTime}, so under {subRuleTime}");

            return subRule;
        }

        return null;
    }
}