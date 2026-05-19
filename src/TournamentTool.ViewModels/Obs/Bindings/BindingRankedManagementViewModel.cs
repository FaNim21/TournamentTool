using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.ViewModels.Obs.Bindings;

public class BindingRankedManagementViewModel : BindingViewModelBase
{
    public BindingRankedManagementViewModel(IDispatcherService dispatcher) : base(dispatcher)
    {
    }

    public override BindingKey GetBindingKey()
    {
        return BindingKey.CreateRankedManagement(ChosenField);
    }
}