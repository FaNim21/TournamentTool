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
         
        _panel.RemoveLeaderboardEntry(entry);
    }
}