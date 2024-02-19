using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class RemovePlayerCommand : BaseCommand
{
    public PlayerManagerViewModel PlayerManagerViewModel { get; set; }

    public RemovePlayerCommand(PlayerManagerViewModel playerManagerViewModel)
    {
        PlayerManagerViewModel = playerManagerViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null || PlayerManagerViewModel == null) return;
        if (parameter is not Player player) return;

        PlayerManagerViewModel.Tournament!.Players.Remove(player);
    }
}
