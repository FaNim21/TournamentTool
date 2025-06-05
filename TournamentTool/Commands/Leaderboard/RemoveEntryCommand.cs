using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Commands.Leaderboard;

public class RemoveEntryCommand : BaseCommand
{
    private LeaderboardPanelViewModel _panel;

    
    public RemoveEntryCommand(LeaderboardPanelViewModel panel)
    {
        _panel = panel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;
        if (DialogBox.Show($"Are you sure you want to remove \"{entry.PlayerUUID}\" entry in leaderboard?",
                "Removing leaderboard entry", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes) return;
        
        _panel.RemoveLeaderboardEntry(entry);
        _panel.RefreshAllEntries();
    }
}