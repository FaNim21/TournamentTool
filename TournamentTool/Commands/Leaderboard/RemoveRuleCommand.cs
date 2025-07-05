using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;

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
        if (DialogBox.Show($"Are you sure you want to remove this rule: {rule.Name}",
                "Removing rul", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes) return;
        
        _panel.RemoveRule(rule);
    }
}