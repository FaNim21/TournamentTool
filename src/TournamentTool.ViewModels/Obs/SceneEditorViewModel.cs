using TournamentTool.Core.Interfaces;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.ViewModels.Obs;

public class SceneEditorViewModel : SceneCanvasViewModel
{
    protected override bool InEditMode => true;
    
    public SceneEditorViewModel(IObsController obs, ILoggingService logger, IDispatcherService dispatcher, IWindowService windowService, ISceneManager sceneManager) 
        : base(obs, logger, dispatcher, sceneManager)
    {
        Setup(null, windowService);
    }
}