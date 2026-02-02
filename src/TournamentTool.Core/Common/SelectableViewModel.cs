using TournamentTool.Core.Interfaces;

namespace TournamentTool.Core.Common;

public class SelectableViewModel : BaseViewModel
{
    public SelectableViewModel(IDispatcherService dispatcher) : base(dispatcher) { }

    public virtual bool CanEnable() => true;
}
