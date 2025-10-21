using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Enums;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class RemoveEntryCommand : BaseCommand
{
    private readonly IDialogService _dialogService;
    private LeaderboardPanelViewModel _panel;


    public RemoveEntryCommand(LeaderboardPanelViewModel panel, IDialogService dialogService)
    {
        _panel = panel;
        _dialogService = dialogService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;
        if (_dialogService.Show($"Are you sure you want to remove \"{entry.PlayerUUID}\" entry in leaderboard?",
                "Removing leaderboard entry", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        
        _panel.RemoveLeaderboardEntry(entry);
        _panel.RefreshAllEntries();
    }
}