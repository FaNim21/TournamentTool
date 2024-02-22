using TournamentTool.ViewModels;

namespace TournamentTool.Commands.PlayerManager;

public class GetCSVPlayersDataCommand : BaseCommand
{
    public PlayerManagerViewModel PlayerManagerViewModel { get; set; }

    public GetCSVPlayersDataCommand(PlayerManagerViewModel viewModel)
    {
        PlayerManagerViewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (PlayerManagerViewModel == null) return;

        //TODO: Czytac plik csv

    }
}
