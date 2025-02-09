using System.IO;
using System.Text.Json;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class DuplicatePresetCommand : BaseCommand
{
    public PresetManagerViewModel PresetManager { get; set; }
    private IPresetSaver _PresetSaver { get; }

    
    public DuplicatePresetCommand(PresetManagerViewModel presetManager, IPresetSaver presetSaver)
    {
        PresetManager = presetManager;
        _PresetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        if (parameter == null) return;
        if (parameter is not TournamentPreset tournament) return;

        string name = tournament.Name;
        name = Helper.GetUniqueName(name, name, PresetManager.IsPresetNameUnique);

        string duplicatePath = Path.Combine(Consts.PresetsPath, name + ".json");
        string originalPath = tournament.GetPath();
        File.Copy(originalPath, duplicatePath);

        string text = File.ReadAllText(duplicatePath) ?? string.Empty;
        try
        {
            if (string.IsNullOrEmpty(text)) return;
            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;
            data.Name = name;
            _PresetSaver.SavePreset(data);

            TournamentPreset preset = new(name);
            PresetManager.AddItem(preset, false);
        }
        catch { }
    }
}
