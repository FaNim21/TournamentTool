using System.IO;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class RemovePresetCommand : BaseCommand
{
    public PresetManagerViewModel MainViewModel { get; set; }

    public RemovePresetCommand(PresetManagerViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not Tournament tournament) return;

        var result = DialogBox.Show($"Are you sure you want to delete: {tournament.Name}", "Deleting", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            MainViewModel.Presets.Remove(tournament);
            File.Delete(tournament.GetPath());
        }
    }
}
