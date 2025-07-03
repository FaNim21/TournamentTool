using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

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
        if (parameter is not PlayerViewModel playerViewModel) return;

        Player playerModel = new()
        {
            Id = playerViewModel.Id,
            Name = playerViewModel.Name,
            InGameName = playerViewModel.InGameName,
            PersonalBest = playerViewModel.PersonalBest,
            TeamName = playerViewModel.TeamName,
            StreamData =
            {
                Main = playerViewModel.StreamData.Main ?? string.Empty,
                Alt = playerViewModel.StreamData.Alt ?? string.Empty
            }
        };

        PlayerManager.AddPlayer(playerModel);
    }
}
