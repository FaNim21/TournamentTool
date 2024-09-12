using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class AddNewPresetCommand : BaseCommand
{
    public PresetManagerViewModel PresetManager { get; set; }

    public AddNewPresetCommand(PresetManagerViewModel presetManager)
    {
        PresetManager = presetManager;
    }

    public override void Execute(object? parameter)
    {
        if (PresetManager == null) return;

        string name = "New Preset";
        name = Helper.GetUniqueName(name, name, PresetManager.IsPresetNameUnique);

        var newPreset = new TournamentPreset(name);
        PresetManager.AddItem(newPreset);
        PresetManager.SetPresetAsNotSaved();
    }
}
