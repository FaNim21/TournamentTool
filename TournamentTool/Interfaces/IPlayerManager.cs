using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Interfaces;

public interface IPlayerManager
{
    void Add(PlayerViewModel playerViewModel);
}