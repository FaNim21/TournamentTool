﻿using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class ClearPresetCommand : BaseCommand
{
    public PresetManagerViewModel PresetManager { get; set; }

    public ClearPresetCommand(PresetManagerViewModel presetManager)
    {
        PresetManager = presetManager;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not IPreset tournament) return;

        var result = DialogBox.Show($"Are you sure you want to clear: {tournament.Name}", "Clearing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes) PresetManager.LoadedPreset!.Clear();

        PresetManager.LoadedPreset = PresetManager.LoadedPreset;
    }
}
