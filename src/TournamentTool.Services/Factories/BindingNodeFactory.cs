using TournamentTool.Domain.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Services.Factories;

public class BindingNodeFactory : IBindingNodeFactory
{
    private readonly ILoggingService _logger;
    private readonly IBindingDataGetter _bindingDataGetter;

    
    public BindingNodeFactory(ILoggingService logger, IBindingDataGetter bindingDataGetter)
    {
        _logger = logger;
        _bindingDataGetter = bindingDataGetter;
    }

    public BindingNode Create(BindingKey key)
    {
        return new BindingNode(key, _logger, _bindingDataGetter);
    }
}