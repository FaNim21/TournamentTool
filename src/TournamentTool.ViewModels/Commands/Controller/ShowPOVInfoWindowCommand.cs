using TournamentTool.Core.Interfaces;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Commands.Controller;

public class ShowPOVInfoWindowCommand : BaseCommand
{
    private readonly IWindowService _windowService;
    private readonly SceneViewModel _sceneViewModel;
    private readonly IDispatcherService _dispatcher;


    //TODO: 1 Rewrite this to not contain this things in scene and just make in in scene controller or something like that
    public ShowPOVInfoWindowCommand(IWindowService windowService, SceneViewModel sceneViewModel, IDispatcherService dispatcher)
    {
        _windowService = windowService;
        _sceneViewModel = sceneViewModel;
        _dispatcher = dispatcher;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PointOfViewViewModel pov) return;

        POVInformationViewModel viewModel = new(pov, _sceneViewModel, _dispatcher);
        _windowService.ShowCustomDialog(viewModel);
    }
}
