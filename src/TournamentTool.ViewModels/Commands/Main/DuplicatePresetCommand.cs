using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.Domain.Interfaces;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Commands.Main;

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
        if (parameter is not TournamentPresetViewModel tournament) return;

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

            TournamentPreset preset = new TournamentPreset(name);
            PresetManager.AddItem(preset);
        }
        catch { /**/ }
    }
}
