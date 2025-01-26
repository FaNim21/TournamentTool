using System.Diagnostics;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.OBS;

public class PreviewScene : Scene
{
    public string TransitionSceneName { get; set; } = string.Empty;


    public PreviewScene(ControllerViewModel controllerViewModel, MainViewModel mainViewModel) : base(controllerViewModel, mainViewModel)
    {
        Type = SceneType.Preview;
    }

    public override async Task GetCurrentSceneItems(string scene, bool force = false, bool updatePlayersInPov = true)
    {
        Trace.WriteLine($"LOADING PREVIEW: {scene}, current: {SceneName}, main: {Controller.MainScene.SceneName}");
        if (Controller.MainScene.SceneName!.Equals(scene))
        {
            if (!Controller.OBS.IsConnectedToWebSocket) return;

            MainText = "NOT SUPPORTED";
            Clear();
            SetSceneName(scene);
            await Controller.OBS.Client.SetCurrentPreviewScene(scene);
            return;
        }

        MainText = string.Empty;
        TransitionSceneName = string.Empty;
        await base.GetCurrentSceneItems(scene, force, false);
    }
}
