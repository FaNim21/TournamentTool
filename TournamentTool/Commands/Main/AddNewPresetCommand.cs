using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class AddNewPresetCommand : BaseCommand
{
    public PresetManagerViewModel MainViewModel { get; set; }

    public AddNewPresetCommand(PresetManagerViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        if (MainViewModel == null) return;

        string name = "New Preset";
        name = Helper.GetUniqueName(name, name, MainViewModel.IsPresetNameUnique);

        var newPreset = new Tournament(MainViewModel, name);
        MainViewModel.AddItem(newPreset);
        MainViewModel.SetPresetAsNotSaved();
    }
}
