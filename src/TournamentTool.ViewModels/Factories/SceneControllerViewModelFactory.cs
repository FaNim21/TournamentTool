using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Factories;

public interface ISceneControllerViewModelFactory
{
    SceneControllerViewModel Create(bool inEditMode = true);
}

public class SceneControllerViewModelFactory : ISceneControllerViewModelFactory
{
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IObsController _obs;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;

    
    public SceneControllerViewModelFactory(ITournamentPlayerRepository playerRepository, IObsController obs, 
        ILoggingService logger, ISettingsProvider settingsProvider, IDispatcherService dispatcher, IWindowService windowService)
    {
        _playerRepository = playerRepository;
        _obs = obs;
        _logger = logger;
        _settingsProvider = settingsProvider;
        _dispatcher = dispatcher;
        _windowService = windowService;
    }

    public SceneControllerViewModel Create(bool inEditMode = true)
    {
        return new SceneControllerViewModel(_obs, _playerRepository, _logger, _settingsProvider, _dispatcher, _windowService, inEditMode);
    }
}