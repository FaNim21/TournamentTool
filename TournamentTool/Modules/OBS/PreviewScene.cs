using System.Diagnostics;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.OBS;

public class PreviewScene : Scene
{
    public string TransitionSceneName { get; set; } = string.Empty;


    public PreviewScene(ControllerViewModel controllerViewModel) : base(controllerViewModel)
    {
        SceneType = "Preview";
    }

    public override async Task GetCurrentSceneItems(string scene, bool force = false)
    {
        Trace.WriteLine($"LOADING PREVIEW: {scene}, current: {SceneName}, main: {Controller.MainScene.SceneName}");
        if (Controller.MainScene.SceneName!.Equals(scene))
        {
            MainText = "NOT SUPPORTED";
            Clear();
            SetSceneName(scene);
            return;
        }

        MainText = string.Empty;
        TransitionSceneName = string.Empty;
        await base.GetCurrentSceneItems(scene, force);
    }

}
