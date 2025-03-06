using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Commands.Main;

public class ClearPresetCommand : BaseCommand
{
    public TournamentViewModel Tournament { get; set; }

    public ClearPresetCommand(TournamentViewModel tournament)
    {
        Tournament = tournament;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not IPreset tournament) return;

        var result = DialogBox.Show($"Are you sure you want to clear: {tournament.Name}", "Clearing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) Tournament.Clear();
    }
}
