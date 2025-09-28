using Microsoft.Extensions.DependencyInjection;
using TournamentTool.Enums;
using TournamentTool.Services.Background;

namespace TournamentTool.Factories;

public class BackgroundServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public BackgroundServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IBackgroundService? Create(ControllerMode mode) =>
        mode switch
        {
            ControllerMode.Paceman => ActivatorUtilities.CreateInstance<PaceManService>(_serviceProvider),
            ControllerMode.Ranked => ActivatorUtilities.CreateInstance<RankedService>(_serviceProvider),
            ControllerMode.Solo => ActivatorUtilities.CreateInstance<SoloService>(_serviceProvider),
            _ => null,
        };
}