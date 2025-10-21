using TournamentTool.Domain.Entities;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.PlayerManager;

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
                Alt = playerViewModel.StreamData.Alt ?? string.Empty,
                Other = playerViewModel.StreamData.Other ?? string.Empty,
                OtherType = playerViewModel.StreamData.OtherType
            }
        };

        PlayerManager.AddPlayer(playerModel);
    }
}
