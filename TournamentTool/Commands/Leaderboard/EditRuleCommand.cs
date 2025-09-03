using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Leaderboard;

public class EditRuleCommand : BaseCommand
{
    private readonly ILuaScriptsManager _luaScriptsManager;
    private IDialogWindow _dialogWindow;
    private TournamentViewModel _tournament;


    public EditRuleCommand(IDialogWindow dialogWindow, TournamentViewModel tournament, ILuaScriptsManager luaScriptsManager)
    {
        _dialogWindow = dialogWindow;
        _tournament = tournament;
        _luaScriptsManager = luaScriptsManager;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not LeaderboardRuleViewModel rule) return;

        rule.FilterSplitsAndAdvancements(_tournament.ControllerMode);
        LeaderboardRuleEditViewModel viewModel = new(rule, _luaScriptsManager, _tournament);
        LeaderboardRuleEditWindow window = new() { DataContext = viewModel };
        
        _dialogWindow.ShowDialog(window);
    }
}