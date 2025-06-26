using TournamentTool.Models;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Interfaces;

public interface IBackgroundDataReceiver;

public interface IPacemanDataReceiver : IBackgroundDataReceiver
{
    void AddPace(Paceman paceman);
    void Update();
    void Remove(Paceman paceman);
    
    void FilterItems();
}

public interface IRankedDataReceiver : IBackgroundDataReceiver
{
    void AddPace(RankedPace pace);
    void Update();
    void Clear();
}

public interface IRankedManagementDataReceiver : IBackgroundDataReceiver
{
    void UpdateManagementData(List<PrivRoomBestSplit> bestSplits, int completedCount, long timeStarted, int playersCount);
}

public interface IPlayerAddReceiver : IBackgroundDataReceiver
{
    void Add(PlayerViewModel playerViewModel);
}
