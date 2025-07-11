﻿using System.IO;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Commands.Main;

public class RemovePresetCommand : BaseCommand
{
    private PresetManagerViewModel PresetManager { get; }

    public RemovePresetCommand(PresetManagerViewModel presetManager)
    {
        PresetManager = presetManager;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not TournamentPreset tournament) return;

        var result = DialogBox.Show($"Are you sure you want to delete: {tournament.Name}", "Deleting", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        PresetManager.RemoveItem(tournament);
    }
}
