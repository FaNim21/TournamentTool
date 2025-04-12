using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class ViewEntryCommand : BaseCommand
{
    private IDialogWindow _dialogWindow;

    
    public ViewEntryCommand(IDialogWindow dialogWindow)
    {
        _dialogWindow = dialogWindow;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        LeaderboardEntryViewViewModel viewModel = new(entry);
        LeaderboardEntryViewWindow window = new() { DataContext = viewModel };
        
        _dialogWindow.ShowDialog(window);
    }
}