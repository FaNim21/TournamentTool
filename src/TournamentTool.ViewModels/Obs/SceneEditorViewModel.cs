using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Obs;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.ViewModels.Obs;

public class SceneEditorViewModel : SceneCanvasViewModel
{
    private readonly AppCache _appCache;
    
    protected override bool InEditMode => true;
    
    //TODO: 0 TO spowoduje, ze po przelaczeniu sie na controller (lub wyjsciu z edytora), dobrze bylo by //zresetowac modele oryginalne
    // zeby przeladowac potencjalne zmiany w scenie spowodowane zewnetrzna interakcja od edytora
    //TODO: 0 /\ Mozliwe ze to jednak nie bedzie potrzebne, poniewaz
    private Scene _editableMainScene;
    private Scene _editablePreviewScene;
    
    public SceneEditorViewModel(IObsController obs, ILoggingService logger, IDispatcherService dispatcher, IWindowService windowService, ISceneManager sceneManager,
        AppCache appCache) 
        : base(obs, logger, dispatcher, sceneManager)
    {
        _appCache = appCache;
        _editableMainScene = sceneManager.MainScene.Clone();
        _editablePreviewScene = sceneManager.PreviewScene.Clone();
        
        //TODO: 0 Trzeba zrobic bool'a w scenie i ustawic boola dla sVjjceny pod nowe itemy plus aktualizacje obecnych itemow zeby te itemy nie aktualiowaly sie w 
        // w konfiguratorze, a aktualizowaly tylko sceny z rzeczywistego scene managera, i tak samo w ItemClose zeby lapac scene czy item pod to zeby zaaktualizowac
        // jego zmiany w konfiguratorze
        
        Setup(_editableMainScene, _editablePreviewScene, null, windowService);
    }
    
    public void UpdateBinding(BindingKey key, string sourceUuid)
    {
        SceneItem? foundItem = SceneManager.MainScene.GetItem<SceneItem>(item => item.SourceUUID.Equals(sourceUuid));
        foundItem ??= SceneManager.PreviewScene.GetItem<SceneItem>(item => item.SourceUUID.Equals(sourceUuid));

        foundItem?.UpdateBinding(key);
    }
}