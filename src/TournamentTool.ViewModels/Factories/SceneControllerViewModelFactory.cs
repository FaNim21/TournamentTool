using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;
using TournamentTool.ViewModels.Obs;

namespace TournamentTool.ViewModels.Factories;

public interface ISceneControllerViewModelFactory
{
    SceneControllerViewModel Create(bool inEditMode = true, bool isStudioModeSupported = true);
}

public class SceneControllerViewModelFactory : ISceneControllerViewModelFactory
{
    private readonly IObsController _obs;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IDispatcherService _dispatcher;
    private readonly IWindowService _windowService;
    private readonly ISceneManager _sceneManager;


    public SceneControllerViewModelFactory(IObsController obs, ILoggingService logger, ISettingsProvider settingsProvider, 
        IDispatcherService dispatcher, IWindowService windowService, ISceneManager sceneManager)
    {
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
        if (inEditMode)
        {
            //To by musialabyc klasa, ktora sluzy do edycji wszystkich danych z obs'a, bez ingerencji 
            
        }
        
        return new SceneControllerViewModel(_obs, _logger, _settingsProvider, _dispatcher, _windowService, _sceneManager, inEditMode, isStudioModeSupported);
    }
}