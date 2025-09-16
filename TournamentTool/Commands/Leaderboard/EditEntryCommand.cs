using TournamentTool.Interfaces;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class EditEntryCommand : BaseCommand
{
    private readonly IDialogWindow _dialogWindow;
    private readonly TournamentViewModel _tournament;
    private readonly LeaderboardPanelViewModel _leaderboardPanelViewModel;
    private readonly INotifyPresetModification _notifyPresetModification;


    public EditEntryCommand(IDialogWindow dialogWindow, TournamentViewModel tournament, LeaderboardPanelViewModel leaderboardPanelViewModel, INotifyPresetModification notifyPresetModification)
    {
        _dialogWindow = dialogWindow;
        _tournament = tournament;
        _leaderboardPanelViewModel = leaderboardPanelViewModel;
        _notifyPresetModification = notifyPresetModification;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        LeaderboardEntryEditViewModel editViewModel = new LeaderboardEntryEditViewModel(_tournament, _leaderboardPanelViewModel, entry.GetLeaderboardEntry(), entry.Player, _notifyPresetModification);
        editViewModel.SetPresetFilters(_tournament.ControllerMode);
        
        LeaderboardEntryEditWindow window = new() { DataContext = editViewModel };
        _dialogWindow.ShowDialog(window);
    }
}