using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.Selectable.Preset;

namespace TournamentTool.ViewModels.Commands.Main;

public class PresetCollectionOrderChangeCommand : BaseCommand
{
    private readonly PresetManagerViewModel _presetManagerViewModel;

    
    public PresetCollectionOrderChangeCommand(PresetManagerViewModel presetManagerViewModel)
    {
        _presetManagerViewModel = presetManagerViewModel;
    }
    
    public override void Execute(object? parameter)
    {
        if (parameter == null) return;

        var elements = (object[])parameter;
        if (elements.Length != 2) return;
        
        TournamentPresetViewModel? insertedItem = null;
        TournamentPresetViewModel? targetedItem = null;

        if (elements[0] is TournamentPresetViewModel insertedPreset)
            insertedItem = insertedPreset;
        if (elements[1] is TournamentPresetViewModel targetedPreset)
            targetedItem = targetedPreset;

        if (insertedItem == null || targetedItem == null) return;
        if (insertedItem == targetedItem) return;

        int oldIndex = _presetManagerViewModel.Presets.IndexOf(insertedItem);
        int nextIndex = _presetManagerViewModel.Presets.IndexOf(targetedItem);

        if (oldIndex != -1 && nextIndex != -1)
        {
            _presetManagerViewModel.Presets.Move(oldIndex, nextIndex);
        }
    }
}