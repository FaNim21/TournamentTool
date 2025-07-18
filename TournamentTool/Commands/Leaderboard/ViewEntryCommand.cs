﻿using TournamentTool.Interfaces;
using TournamentTool.ViewModels.Ranking;
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

        entry.SetupOpeningWindow();
        LeaderboardEntryViewWindow window = new() { DataContext = entry };
        _dialogWindow.ShowDialog(window);
    }
}