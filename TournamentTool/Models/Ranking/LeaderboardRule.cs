﻿using TournamentTool.Enums;

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
}