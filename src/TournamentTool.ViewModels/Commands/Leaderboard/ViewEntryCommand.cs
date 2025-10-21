using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Ranking;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class ViewEntryCommand : BaseCommand
{
    private readonly IWindowService _windowService;


    public ViewEntryCommand(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        entry.SetupOpeningWindow();
        _windowService.ShowDialog(entry, null, "LeaderboardEntryWindow");
    }
}