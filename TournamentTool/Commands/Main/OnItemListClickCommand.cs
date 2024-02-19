using TournamentTool.ViewModels;

namespace TournamentTool.Commands.Main;

public class OnItemListClickCommand : BaseCommand
{
    public MainViewModel MainViewModel { get; set; }

    public OnItemListClickCommand(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
    }

    public override void Execute(object? parameter)
    {
        MainViewModel.SavePresetCommand.Execute(parameter);
    }
}
