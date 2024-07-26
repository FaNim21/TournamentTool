using TournamentTool.ViewModels;

namespace TournamentTool.Modules.OBS;

public class PreviewScene : Scene
{
    public string TransitionSceneName { get; set; } = string.Empty;


    public PreviewScene(ControllerViewModel controllerViewModel) : base(controllerViewModel)
    {

    }

    public override async Task GetCurrentSceneItems(string scene, bool force = false)
    {
        if (Controller.MainScene.SceneName!.Equals(scene) && string.IsNullOrEmpty(TransitionSceneName))
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
