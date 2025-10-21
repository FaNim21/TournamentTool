using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class RemoveRuleCommand : BaseCommand
{
    private LeaderboardPanelViewModel _panel;
    private readonly IDialogService _dialogService;


    public RemoveRuleCommand(LeaderboardPanelViewModel panel, IDialogService dialogService)
    {
        _panel = panel;
        _dialogService = dialogService;
    }
    
    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;
        if (_dialogService.Show($"Are you sure you want to remove this rule: {rule.Name}",
                "Removing rul", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        
        _panel.RemoveRule(rule);
    }
}