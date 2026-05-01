using TournamentTool.ViewModels.Obs.Items;

namespace TournamentTool.ViewModels.Obs;

//TODO: 0 Zrobic to nie tylko pov, ale ogolnie do itemow, ktore wisza na scenie, tak zeby dalo sie wejsc w interkacje z poziomu code behind/behavior scene canvas
public interface IScenePovInteractable
{
    void OnPOVClick(SceneViewModel sceneViewModel, PointOfViewViewModel clickedPov);
    void UnSelectItems(bool clearAll = false);
}