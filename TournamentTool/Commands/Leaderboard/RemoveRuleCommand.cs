using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Commands.Leaderboard;

public class RemoveRuleCommand : BaseCommand
{
    private LeaderboardPanelViewModel _panel;


    public RemoveRuleCommand(LeaderboardPanelViewModel panel)
    {
        _panel = panel;
    }
    
    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;
        
        _panel.RemoveRule(rule);
    }
}