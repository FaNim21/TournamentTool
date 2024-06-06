using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class OnItemListClickCommand : BaseCommand
{
    public PresetManagerViewModel MainViewModel { get; set; }

    public OnItemListClickCommand(PresetManagerViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        MainViewModel.SavePresetCommand.Execute(parameter);
    }
}
