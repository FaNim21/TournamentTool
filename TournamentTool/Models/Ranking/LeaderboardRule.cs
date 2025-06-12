using TournamentTool.Enums;

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
        if (data.MainSplit.Milestone != ChosenAdvancement) return null;
        
        foreach (var subRule in SubRules)
        {
            if (!subRule.Evaluate(data)) continue; 
            return subRule;
        }

        return null;
    }

    public void Move(int oldIndex, int newIndex)
    {
        var item = SubRules[oldIndex];
        SubRules.RemoveAt(oldIndex);
        SubRules.Insert(newIndex, item);
    }
}