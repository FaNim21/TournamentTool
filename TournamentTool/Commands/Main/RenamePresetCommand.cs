using TournamentTool.Components;
using TournamentTool.Interfaces;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class RenamePresetCommand : BaseCommand
{
    public IPresetSaver PresetSaver { get; }

    public RenamePresetCommand(IPresetSaver presetSaver)
    {
        PresetSaver = presetSaver;
    }

    public override void Execute(object? parameter)
    {
        PresetSaver.SavePreset();

        EditableTextBlock? textBlock = Helper.GetFocusedUIElement<EditableTextBlock>();
        if (textBlock != null && textBlock.IsEditable)
            textBlock.IsInEditMode = true;
    }
}
