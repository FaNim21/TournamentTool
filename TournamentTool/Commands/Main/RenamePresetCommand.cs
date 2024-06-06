using TournamentTool.Components;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class RenamePresetCommand : BaseCommand
{
    public PresetManagerViewModel MainViewModel { get; set; }

    public RenamePresetCommand(PresetManagerViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        MainViewModel.SavePresetCommand.Execute(MainViewModel);

        EditableTextBlock? textBlock = Helper.GetFocusedUIElement<EditableTextBlock>();
        if (textBlock != null && textBlock.IsEditable)
            textBlock.IsInEditMode = true;
    }
}
