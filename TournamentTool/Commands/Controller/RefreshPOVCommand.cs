using TournamentTool.Models;

namespace TournamentTool.Commands.Controller;

public class RefreshPOVCommand : BaseCommand
{
    public RefreshPOVCommand() { }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not PointOfView pov) return;

        pov.RefreshCommand.Execute(null);
    }
}
