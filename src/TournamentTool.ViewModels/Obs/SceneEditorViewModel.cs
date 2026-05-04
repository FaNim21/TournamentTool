using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.ViewModels.Obs;

public class SceneEditorViewModel : SceneCanvasViewModel
{
    private readonly AppCache _appCache;
    
    protected override bool InEditMode => true;
    
    private Scene _editableMainScene;
    private Scene _editablePreviewScene;
    
    public SceneEditorViewModel(IObsController obs, ILoggingService logger, IDispatcherService dispatcher, IWindowService windowService, ISceneManager sceneManager,
        AppCache appCache) 
        : base(obs, logger, dispatcher, sceneManager)
    {
        _appCache = appCache;
        _editableMainScene = sceneManager.MainScene.Clone();
        _editablePreviewScene = sceneManager.PreviewScene.Clone();
        
        Setup(_editableMainScene, _editablePreviewScene, null, windowService);
    }
    
    public async Task UpdateScenes(string sourceUuid)
    {
        await _editableMainScene.RefreshAsync();

        SceneItem? foundItem = SceneManager.MainScene.GetItem<SceneItem>(item => item.SourceUUID.Equals(sourceUuid));
        if (foundItem is { })
        {
            await SceneManager.MainScene.RefreshAsync();
        }
        else
        {
            foundItem ??= SceneManager.PreviewScene.GetItem<SceneItem>(item => item.SourceUUID.Equals(sourceUuid));
            if (foundItem is { })
            {
                await SceneManager.PreviewScene.RefreshAsync();
            }
        }

        MainSceneViewModel.Refresh();
    }
}