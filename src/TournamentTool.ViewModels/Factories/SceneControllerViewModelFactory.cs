using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Obs;

namespace TournamentTool.ViewModels.Factories;

public class SceneCanvasViewModelFactory : ISceneControllerViewModelFactory
{
    private readonly IObsController _obs;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;
    private readonly ISceneManager _sceneManager;


    public SceneCanvasViewModelFactory(IObsController obs, ILoggingService logger, ISettingsProvider settingsProvider,
        IDispatcherService dispatcher, IWindowService windowService, ISceneManager sceneManager)
    {
        _obs = obs;
        _logger = logger;
        _settingsProvider = settingsProvider;
        _dispatcher = dispatcher;
        _windowService = windowService;
        _sceneManager = sceneManager;
    }

    public SceneRuntimeViewModel CreateRuntime()
    {
        return new SceneRuntimeViewModel(_obs, _logger, _dispatcher, _windowService, _sceneManager);
    }

    public SceneEditorViewModel CreateEditor()
    {
        return new SceneEditorViewModel(_obs, _logger, _dispatcher, _windowService, _sceneManager,
            _settingsProvider.Get<AppCache>());
    }
}