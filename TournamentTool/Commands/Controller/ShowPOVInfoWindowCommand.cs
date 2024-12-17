using TournamentTool.Models;
using TournamentTool.Modules.OBS;

namespace TournamentTool.Commands.Controller;

public class ShowPOVInfoWindowCommand : BaseCommand
{
    private Scene _scene;

    public ShowPOVInfoWindowCommand(Scene scene) : base()
    {
        _scene = scene;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not PointOfView pov) return;

        _scene.OpenPOVInfoWindow(pov);
    }
}
