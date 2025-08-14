using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;
using TournamentTool.ViewModels;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Controller;

public class ShowPOVInfoWindowCommand : BaseCommand
{
    private readonly IDialogWindow _dialogWindow;
    private readonly Scene _scene;


    public ShowPOVInfoWindowCommand(IDialogWindow dialogWindow, Scene scene)
    {
        _dialogWindow = dialogWindow;
        _scene = scene;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PointOfView pov) return;

        POVInformationViewModel viewModel = new(pov, _scene);
        POVInformationWindow window = new() { DataContext = viewModel };

        _dialogWindow.ShowDialog(window);
    }
}
