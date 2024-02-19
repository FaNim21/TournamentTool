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

        PlayerManagerViewModel.IsEditing = true;
        PlayerManagerViewModel.Player = player;
    }
}
