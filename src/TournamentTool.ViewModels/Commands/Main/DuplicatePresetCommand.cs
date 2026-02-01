using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Commands.Main;

public class DuplicatePresetCommand : BaseCommand
{
    private ILoggingService Logger { get; }
    public PresetManagerViewModel PresetManager { get; set; }


    public DuplicatePresetCommand(PresetManagerViewModel presetManager, ILoggingService logger)
    {
        PresetManager = presetManager;
        Logger = logger;
    }

    public override void Execute(object? parameter)
    {
        if (parameter is not TournamentPresetViewModel tournament) return;

        string name = tournament.Name;
        name = Helper.GetUniqueName(name, name, PresetManager.IsPresetNameUnique);

        string duplicatePath = Path.Combine(Consts.PresetsPath, $"{name}.json");
        string originalPath = tournament.GetPath();
        
        PresetManager.SwitchFileWatcher(false);
        File.Copy(originalPath, duplicatePath);
        PresetManager.SwitchFileWatcher(true);
        
        string text = File.ReadAllText(duplicatePath) ?? string.Empty;
        
        try
        {
            if (string.IsNullOrEmpty(text)) return;

            Tournament? data = JsonSerializer.Deserialize<Tournament>(text);
            if (data == null) return;

            data.Name = name;
            PresetManager.AddDuplicatedItem(data);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}
