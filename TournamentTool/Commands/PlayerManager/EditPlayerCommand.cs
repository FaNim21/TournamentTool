using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class EditPlayerCommand : BaseCommand
{
    private PlayerManagerViewModel PlayerManager { get; }


    public EditPlayerCommand(PlayerManagerViewModel playerManager)
    {
        PlayerManager = playerManager;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not Player player) return;

        Player newPlayer = new()
        {
            Id = player.Id,
            Name = player.Name,
            InGameName = player.InGameName,
            PersonalBest = player.PersonalBest,
            TeamName = player.TeamName,
        };
        newPlayer.StreamData.Main = player.StreamData.Main ?? string.Empty;
        newPlayer.StreamData.Alt = player.StreamData.Alt ?? string.Empty;

        PlayerManager.AddPlayer(newPlayer);
    }
}
