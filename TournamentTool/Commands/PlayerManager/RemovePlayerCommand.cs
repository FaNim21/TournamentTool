using TournamentTool.Interfaces;
using TournamentTool.Models;

namespace TournamentTool.Commands.PlayerManager;

public class RemovePlayerCommand : BaseCommand
{
    private readonly ITournamentManager _tournamentManager;
    private readonly IPresetSaver _presetSaver;

    
    public RemovePlayerCommand(ITournamentManager tournamentManager, IPresetSaver presetSaver)
    {
        _tournamentManager = tournamentManager;
        _presetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not Player player) return;

        _tournamentManager.RemovePlayer(player);
        _presetSaver.SavePreset();
    }
}
