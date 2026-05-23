using TournamentTool.Domain.Entities;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Services.Factories;

public interface IManagementDataContextFactory
{
    IManagementDataContext<TManagement> Create<TManagement>() where TManagement : ManagementData, new();
}

public class ManagementDataContextFactory : IManagementDataContextFactory
{
    private readonly ITournamentState _state;
    private readonly IBindingEngine _bindingEngine;

    public ManagementDataContextFactory(ITournamentState state, IBindingEngine bindingEngine)
    {
        _state = state;
        _bindingEngine = bindingEngine;
    }

    public IManagementDataContext<TManagement> Create<TManagement>() where TManagement : ManagementData, new()
        => new ManagementDataContext<TManagement>(_state, _bindingEngine);
}