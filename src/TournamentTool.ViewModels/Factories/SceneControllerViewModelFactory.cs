using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Factories;

public interface ISceneControllerViewModelFactory
{
    SceneControllerViewModelViewModel Create(bool inEditMode = true);
}

public class SceneControllerViewModelFactory : ISceneControllerViewModelFactory
{
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IObsController _obs;
    private readonly IBindingEngine _bindingEngine;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;

    
    public SceneControllerViewModelFactory(ITournamentPlayerRepository playerRepository, IObsController obs, IBindingEngine bindingEngine, 
        ILoggingService logger, ISettingsProvider settingsProvider, IDispatcherService dispatcher, IWindowService windowService)
    {
        _playerRepository = playerRepository;
        _obs = obs;
        _bindingEngine = bindingEngine;
        _logger = logger;
        _settingsProvider = settingsProvider;
        _dispatcher = dispatcher;
        _windowService = windowService;
    }

    public SceneControllerViewModelViewModel Create(bool inEditMode = true)
    {
        return new SceneControllerViewModelViewModel(_obs, _playerRepository, _logger, _bindingEngine, _settingsProvider, _dispatcher, 
            _windowService, inEditMode);
    }
}