using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Commands.Controller;

public class ShowPOVInfoWindowCommand : BaseCommand
{
    private readonly IWindowService _windowService;
    private readonly Scene _scene;
    private readonly IDispatcherService _dispatcher;


    public ShowPOVInfoWindowCommand(IWindowService windowService, Scene scene, IDispatcherService dispatcher)
    {
        _windowService = windowService;
        _scene = scene;
        _dispatcher = dispatcher;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PointOfView pov) return;

        POVInformationViewModel viewModel = new(pov, _scene, _dispatcher);
        _windowService.ShowCustomDialog(viewModel);
    }
}
