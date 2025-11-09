using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class EditEntryCommand : BaseCommand
{
    private readonly IWindowService _windowService;
    private readonly ITournamentState _tournamentState;
    private readonly LeaderboardPanelViewModel _leaderboardPanelViewModel;
    private readonly IDispatcherService _dispatcher;
    private readonly IDialogService _dialogService;


    public EditEntryCommand(IWindowService windowService, ITournamentState tournamentState, LeaderboardPanelViewModel leaderboardPanelViewModel, IDispatcherService dispatcher, IDialogService dialogService)
    {
        _windowService = windowService;
        _tournamentState = tournamentState;
        _leaderboardPanelViewModel = leaderboardPanelViewModel;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        LeaderboardEntryEditWindowViewModel editWindowViewModel = new LeaderboardEntryEditWindowViewModel(_leaderboardPanelViewModel, entry.GetLeaderboardEntry(), entry.Player, _tournamentState, _dispatcher, _dialogService);
        editWindowViewModel.SetPresetFilters(_tournamentState.CurrentPreset.ControllerMode);
        
        _windowService.ShowDialog(editWindowViewModel, null, "LeaderboardEntryEditWindow");
    }
}