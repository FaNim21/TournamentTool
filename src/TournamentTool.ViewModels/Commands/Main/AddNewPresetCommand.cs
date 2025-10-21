using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities.Preset;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Commands.Main;

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
