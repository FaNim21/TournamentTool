using TournamentTool.Domain.Enums;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;

namespace TournamentTool.Presentation.Factories;

public interface ISceneFactory
{
    Scene Create(ISceneManager sceneManager, SceneType sceneType);
}