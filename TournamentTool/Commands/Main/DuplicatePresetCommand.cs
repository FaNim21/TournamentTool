using System.IO;
using System.Text.Json;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class DuplicatePresetCommand : BaseCommand
{
    public PresetManagerViewModel PresetManager { get; set; }
    public MainViewModel MainViewModel { get; set; }

    public DuplicatePresetCommand(PresetManagerViewModel presetManager, MainViewModel mainViewModel)
    {
        PresetManager = presetManager;
        MainViewModel = mainViewModel;
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
            MainViewModel.SavePreset(data);

            TournamentPreset preset = new(name);
            PresetManager.AddItem(preset, false);
        }
        catch { }
    }
}
