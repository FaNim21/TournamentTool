using TournamentTool.Domain.Entities.Obs;

namespace TournamentTool.Services.Obs;

public interface ISceneItemGetter
{
    IPointOfView? GetPointOfView(string sourceName);
}