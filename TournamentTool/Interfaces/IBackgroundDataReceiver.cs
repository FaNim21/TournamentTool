using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Interfaces;

public interface IBackgroundDataReceiver;

public interface IPacemanDataReceiver : IBackgroundDataReceiver
{
    void ReceivePlayers(List<PaceManViewModel> players);
    void FilterItems();
}

public interface IRankedDataReceiver : IBackgroundDataReceiver
{
    void ReceivePlayers(List<RankedPace> paces);
}

public interface IPlayerManagerReceiver : IBackgroundDataReceiver
{
    void Add(PlayerViewModel playerViewModel);
}
