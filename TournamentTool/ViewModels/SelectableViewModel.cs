using TournamentTool.Models;
using TournamentTool.Modules;

namespace TournamentTool.ViewModels;

public class SelectableViewModel : BaseViewModel
{
    protected MainViewModelCoordinator Coordinator { get; }

    public object? parameterForNextSelectable;


    public SelectableViewModel(MainViewModelCoordinator coordinator)
    {
        Coordinator = coordinator;
    }

    public virtual bool CanEnable(Tournament tournament)
    {
        return true;
    }

    public void SetParameter(object? parameter)
    {
        parameterForNextSelectable = parameter;
    }
}
