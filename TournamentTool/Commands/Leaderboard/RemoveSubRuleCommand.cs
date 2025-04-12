using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Commands.Leaderboard;

public class RemoveSubRuleCommand : BaseCommand
{
    private LeaderboardRuleEditViewModel _ruleEdit;

    
    public RemoveSubRuleCommand(LeaderboardRuleEditViewModel ruleEdit)
    {
        _ruleEdit = ruleEdit;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardSubRuleViewModel subRule) return;

        _ruleEdit.RemoveSubRule(subRule);
    }
}