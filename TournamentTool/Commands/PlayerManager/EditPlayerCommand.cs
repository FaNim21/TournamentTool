using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class EditPlayerCommand : BaseCommand
{
    public PlayerManagerViewModel PlayerManagerViewModel { get; set; }

    public EditPlayerCommand(PlayerManagerViewModel playerManagerViewModel)
    {
        PlayerManagerViewModel = playerManagerViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null || PlayerManagerViewModel == null) return;
        if (parameter is not Player player) return;

        Player newPlayer = new()
        {
            Id = player.Id,
            Name = player.Name,
            InGameName = player.InGameName,
            PersonalBest = player.PersonalBest,
        };
        newPlayer.StreamData.Main = player.StreamData.Main ?? string.Empty;
        newPlayer.StreamData.Alt = player.StreamData.Alt ?? string.Empty;

        PlayerManagerViewModel.AddPlayer(newPlayer);
    }
}
