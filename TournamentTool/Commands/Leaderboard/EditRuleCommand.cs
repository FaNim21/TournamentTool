using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class EditRuleCommand : BaseCommand
{
    private IDialogWindow _dialogWindow;
    private TournamentViewModel _tournament;

    
    public EditRuleCommand(IDialogWindow dialogWindow, TournamentViewModel tournament)
    {
        _dialogWindow = dialogWindow;
        _tournament = tournament;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;

        rule.FilterSplitsAndAdvancements(_tournament.ControllerMode);
        LeaderboardRuleEditViewModel viewModel = new(rule);
        LeaderboardRuleEditWindow window = new() { DataContext = viewModel };
        
        _dialogWindow.ShowDialog(window);
    }
}