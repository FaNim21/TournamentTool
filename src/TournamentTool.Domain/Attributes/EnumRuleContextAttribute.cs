using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Attributes;

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