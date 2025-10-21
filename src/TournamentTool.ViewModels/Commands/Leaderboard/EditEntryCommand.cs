using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class EditEntryCommand : BaseCommand
{
    private ITournamentPresetManager Tournament { get; }
    private readonly IWindowService _windowService;
    private readonly LeaderboardPanelViewModel _leaderboardPanelViewModel;
    private readonly INotifyPresetModification _notifyPresetModification;
    private readonly IDispatcherService _dispatcher;
    private readonly IDialogService _dialogService;


    public EditEntryCommand(IWindowService windowService, ITournamentPresetManager tournament, LeaderboardPanelViewModel leaderboardPanelViewModel, INotifyPresetModification notifyPresetModification, IDispatcherService dispatcher, IDialogService dialogService)
    {
        _windowService = windowService;
        Tournament = tournament;
        Tournament = tournament;
        _leaderboardPanelViewModel = leaderboardPanelViewModel;
        _notifyPresetModification = notifyPresetModification;
        _dispatcher = dispatcher;
        _dialogService = dialogService;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardEntryViewModel entry) return;

        LeaderboardEntryEditWindowViewModel editWindowViewModel = new LeaderboardEntryEditWindowViewModel(Tournament, _leaderboardPanelViewModel, entry.GetLeaderboardEntry(), entry.Player, _notifyPresetModification, _dispatcher, _dialogService);
        editWindowViewModel.SetPresetFilters(Tournament.ControllerMode);
        
        _windowService.ShowDialog(entry, null, "LeaderboardEntryEditWindow");
    }
}