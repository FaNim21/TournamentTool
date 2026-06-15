using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Presentation.Factories;

public sealed class SceneFactory : ISceneFactory
{
    private readonly ILoggingService _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly AppCache _appCache;

    public SceneFactory(ILoggingService logger, IServiceProvider serviceProvider, ISettingsProvider settingsProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appCache = settingsProvider.Get<AppCache>();
    }
    
    public Scene Create(ISceneManager sceneManager, SceneType sceneType)
    {
        return new Scene(sceneManager, _logger, _appCache, _serviceProvider, sceneType);
    }
}