using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Common;

public class SelectableViewModel : BaseViewModel
{
    protected ICoordinator Coordinator { get; }

    public object? parameterForNextSelectable;


    public SelectableViewModel(ICoordinator coordinator, IDispatcherService dispatcher) : base(dispatcher)
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
