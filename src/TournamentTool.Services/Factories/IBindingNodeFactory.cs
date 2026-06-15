using TournamentTool.Domain.Obs;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Services.Factories;

public interface IBindingNodeFactory
{
    BindingNode Create(BindingKey key);
}