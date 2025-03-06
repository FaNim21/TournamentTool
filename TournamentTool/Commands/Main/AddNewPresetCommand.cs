using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class AddNewPresetCommand : BaseCommand
{
    private PresetManagerViewModel PresetManager { get; }

    public AddNewPresetCommand(PresetManagerViewModel presetManager)
    {
        PresetManager = presetManager;
    }

    public override void Execute(object? parameter)
    {
        string name = "New Preset";
        name = Helper.GetUniqueName(name, name, PresetManager.IsPresetNameUnique);

        var newPreset = new TournamentPreset(name);
        PresetManager.AddItem(newPreset);
    }
}
