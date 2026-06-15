using TournamentTool.Domain.Obs;

namespace TournamentTool.Services.Obs.Binding;

public interface IBindingDataGetter
{
    string GetData(BindingKey key);
}