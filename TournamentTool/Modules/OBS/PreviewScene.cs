using System.Diagnostics;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Controller;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.OBS;

public class PreviewScene : Scene
{
    public string TransitionSceneName { get; set; } = string.Empty;


    public PreviewScene(SceneControllerViewmodel sceneController, IDialogWindow dialogWindow) : base(sceneController, dialogWindow)
    {
        Type = SceneType.Preview;
    }

    public override async Task GetCurrentSceneItems(string scene, bool force = false, bool updatePlayersInPov = true)
    {
        Trace.WriteLine($"LOADING PREVIEW: {scene}, current: {SceneName}, main: {SceneController.MainScene.SceneName}");
        if (SceneController.MainScene.SceneName!.Equals(scene))
        {
            if (!SceneController.OBS.IsConnectedToWebSocket) return;

            MainText = "NOT SUPPORTED";
            Clear();
            SetSceneName(scene);
            await SceneController.OBS.Client.SetCurrentPreviewScene(scene);
            return;
        }

        MainText = string.Empty;
        TransitionSceneName = string.Empty;
        await base.GetCurrentSceneItems(scene, force, false);
    }
}
