using TournamentTool.Interfaces;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class EditEntryCommand : BaseCommand
{
    private readonly IDialogWindow _dialogWindow;

    
    public EditEntryCommand(IDialogWindow dialogWindow)
    {
        _dialogWindow = dialogWindow;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        LeaderboardEntryEditViewModel editViewModel = new LeaderboardEntryEditViewModel(entry.GetLeaderboardEntry(), entry.Player);
        LeaderboardEntryEditWindow window = new() { DataContext = editViewModel };
        _dialogWindow.ShowDialog(window);
    }
}