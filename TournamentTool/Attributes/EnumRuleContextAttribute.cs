using TournamentTool.Enums;
using TournamentTool.Models.Ranking;

namespace TournamentTool.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class EnumRuleContextAttribute : Attribute
{
    public LeaderboardRuleType RuleType { get; private set; }
    public ControllerMode ControllerMode { get; private set; }
    
    
    public EnumRuleContextAttribute(ControllerMode controllerMode, LeaderboardRuleType ruleType)
    {
        ControllerMode = controllerMode;
        RuleType = ruleType;
    }
}