using TournamentTool.Core.Interfaces;
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
        return new SceneEditorViewModel(_obs, _logger, _dispatcher, _windowService, _sceneManager);
    }
    
    //TODO: 0 Moze zrobic dwie rozne klasy z racji tego, ze logika bedzie przeniesiona do SceneController, to wtedy mozna oprzec sie na roznicy przez 
    // in edit mode bool
    public SceneRuntimeViewModel Create(bool inEditMode = true, bool isStudioModeSupported = true)
    {
        //isStudioModeSupported nie bedzie potrzebne, bo w edytowalnym opierac sie wszystko bedzie na liscie scen
        
        //To by musialabyc klasa, ktora sluzy do edycji wszystkich danych z obs'a, bez ingerencji w cos tam juz nie pamietam
        // przeczytaj to do w OnSelectedSceneChanged in SceneManagementViewModel
        
        return null;
    }
}