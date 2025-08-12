using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Windows;

namespace TournamentTool.Commands.Controller;

public class ShowPOVInfoWindowCommand : BaseCommand
{
    private readonly IDialogWindow _dialogWindow;

    
    public ShowPOVInfoWindowCommand(IDialogWindow dialogWindow)
    {
        _dialogWindow = dialogWindow;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not PointOfView pov) return;

        POVInformationViewModel viewModel = new(pov);
        POVInformationWindow window = new() { DataContext = viewModel };

        _dialogWindow.ShowDialog(window);
    }
}
