using TournamentTool.Interfaces;

namespace TournamentTool.ViewModels.Selectable;

public class SelectableViewModel : BaseViewModel
{
    protected ICoordinator Coordinator { get; }

    public object? parameterForNextSelectable;


    public SelectableViewModel(ICoordinator coordinator)
    {
        Coordinator = coordinator;
    }

    public virtual bool CanEnable()
    {
        return true;
    }

    public void SetParameter(object? parameter)
    {
        parameterForNextSelectable = parameter;
    }
}
