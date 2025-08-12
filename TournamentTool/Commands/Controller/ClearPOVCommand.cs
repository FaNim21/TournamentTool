using TournamentTool.Models;

namespace TournamentTool.Commands.Controller;

public class ClearPOVCommand : BaseCommand
{
    public override void Execute(object? parameter)
    {
        if (parameter is not PointOfView pov) return;

        pov.Clear(true);
    }
}
