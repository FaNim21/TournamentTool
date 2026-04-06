using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Factories;

public interface ISceneControllerViewModelFactory
{
    SceneControllerViewModel Create(bool inEditMode = true, bool isStudioModeSupported = true);
}

public class SceneControllerViewModelFactory : ISceneControllerViewModelFactory
{
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IObsController _obs;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;
    private readonly ISceneManager _sceneManager;


    public SceneControllerViewModelFactory(ITournamentPlayerRepository playerRepository, IObsController obs, 
        ILoggingService logger, ISettingsProvider settingsProvider, IDispatcherService dispatcher, IWindowService windowService,
        ISceneManager sceneManager)
    {
        _playerRepository = playerRepository;
        _obs = obs;
        _logger = logger;
        _settingsProvider = settingsProvider;
        _dispatcher = dispatcher;
        _windowService = windowService;
        _sceneManager = sceneManager;
    }

    //TODO: 0 Moze zrobic dwie rozne klasy z racji tego, ze logika bedzie przeniesiona do SceneController, to wtedy mozna oprzec sie na roznicy przez 
    // in edit mode bool
    public SceneControllerViewModel Create(bool inEditMode = true, bool isStudioModeSupported = true)
    {
        return new SceneControllerViewModel(_obs, _logger, _settingsProvider, _dispatcher, _windowService, _sceneManager, inEditMode, 
            isStudioModeSupported);
    }
}