using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Obs;

namespace TournamentTool.ViewModels.Obs.Bindings;

public class BindingLeaderboardViewModel : BindingViewModelBase
{
    public BindingLeaderboardViewModel(IDispatcherService dispatcher) : base(dispatcher)
    {
    }

    public override BindingKey GetBindingKey()
    {
        return BindingKey.CreateLeaderboard(ChosenField, -1);
    }
}