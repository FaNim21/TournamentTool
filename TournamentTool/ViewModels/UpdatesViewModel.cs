using TournamentTool.Models;

namespace TournamentTool.ViewModels;

public class UpdatesViewModel : SelectableViewModel
{


    public UpdatesViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        CanBeDestroyed = false;
    }

    public override bool CanEnable(Tournament tournament)
    {
        return true;
    }

    public override void OnEnable(object? parameter)
    {

    }

    public override bool OnDisable()
    {
        return true;
    }
}
