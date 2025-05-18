using TournamentTool.Models;
using TournamentTool.Modules.SidePanels;
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
    void ReceivePaces(List<RankedPace> paces);
    void UpdateAPIData(List<RankedBestSplit> bestSplits, int completedCount);
}

public interface IPlayerManagerReceiver : IBackgroundDataReceiver
{
    void Add(PlayerViewModel playerViewModel);
}
