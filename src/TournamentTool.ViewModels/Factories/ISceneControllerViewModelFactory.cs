using TournamentTool.ViewModels.Obs;

namespace TournamentTool.ViewModels.Factories;

public interface ISceneControllerViewModelFactory
{
    SceneRuntimeViewModel CreateRuntime();
    SceneEditorViewModel CreateEditor();
}