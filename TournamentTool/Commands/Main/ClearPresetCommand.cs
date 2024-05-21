using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class ClearPresetCommand : BaseCommand
{
    public MainViewModel MainViewModel { get; set; }

    public ClearPresetCommand(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not Tournament tournament) return;

        var result = DialogBox.Show($"Are you sure you want to clear: {tournament.Name}", "Clearing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) tournament.Clear();

        if (tournament == MainViewModel.CurrentChosen)
            MainViewModel.CurrentChosen = tournament;
    }
}
