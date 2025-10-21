using TournamentTool.Core.Interfaces;

namespace TournamentTool.Services.Background;

public interface IBackgroundDataReceiver;

public interface IPacemanDataReceiver : IBackgroundDataReceiver
{
    void AddPace(Paceman paceman);
    void AddPaces(IEnumerable<Paceman> pacemans);
    void Update();
    void Remove(Paceman paceman);
    
    void FilterItems();
}

public interface IRankedDataReceiver : IBackgroundDataReceiver
{
    void AddPace(RankedPace pace);
    void AddPaces(IEnumerable<RankedPace> rankedPaces);
    void Update();
    void Clear();
}

public interface IRankedManagementDataReceiver : IBackgroundDataReceiver
{
    void Update();
}

public interface IPlayerAddReceiver : IBackgroundDataReceiver
{
    void Add(IPlayerViewModel playerViewModel);
}
