using TournamentTool.Core.Interfaces;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;

namespace TournamentTool.ViewModels.Commands.Leaderboard;

public class EditRuleCommand : BaseCommand
{
    private readonly ILuaScriptsManager _luaScriptsManager;
    private readonly IDialogService _dialogService;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;
    private readonly ITournamentState _tournamentState;


    public EditRuleCommand(IWindowService windowService, ITournamentState tournamentState, ILuaScriptsManager luaScriptsManager, IDialogService dialogService, IDispatcherService dispatcher)
    {
        _windowService = windowService;
        _tournamentState = tournamentState;
        _luaScriptsManager = luaScriptsManager;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;

        rule.FilterSplitsAndAdvancements(_tournamentState.CurrentPreset.ControllerMode);
        LeaderboardRuleEditWindowViewModel windowViewModel = new(rule, _luaScriptsManager, _tournamentState, _dialogService, _dispatcher);
        _windowService.ShowDialog(windowViewModel, null, "LeaderboardRuleEditWindow");
    }
}