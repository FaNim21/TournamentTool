using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class EditRuleCommand : BaseCommand
{
    private IDialogWindow _dialogWindow;

    
    public EditRuleCommand(IDialogWindow dialogWindow)
    {
        _dialogWindow = dialogWindow;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;
            
        LeaderboardRuleEditViewModel viewModel = new(rule);
        LeaderboardRuleEditWindow window = new() { DataContext = viewModel };
        
        _dialogWindow.ShowDialog(window);
    }
}