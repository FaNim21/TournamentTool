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
    void AddPace(RankedPace pace);
    void Update();
    void RemovePace(RankedPace pace);

    void ReceiveAllPaces(List<RankedPace> paces);
}

public interface IRankedManagementDataReceiver : IBackgroundDataReceiver
{
    void UpdateManagementData(List<RankedBestSplit> bestSplits, int completedCount, long timeStarted, int playersCount);
}

public interface IPlayerAddReceiver : IBackgroundDataReceiver
{
    void Add(PlayerViewModel playerViewModel);
}
