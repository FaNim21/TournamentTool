using TournamentTool.ViewModels.Ranking;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class RemoveSubRuleCommand : BaseCommand
{
    private LeaderboardRuleEditWindowViewModel _ruleEditWindow;

    
    public RemoveSubRuleCommand(LeaderboardRuleEditWindowViewModel ruleEditWindow)
    {
        _ruleEditWindow = ruleEditWindow;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardSubRuleViewModel subRule) return;

        _ruleEditWindow.RemoveSubRule(subRule);
    }
}