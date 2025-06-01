using TournamentTool.Interfaces;

namespace TournamentTool.Services.Background;

public interface IBackgroundCoordinator
{
    void Register(IBackgroundDataReceiver? receiver);
    void Unregister(IBackgroundDataReceiver? receiver);
    void Initialize(IBackgroundService backgroundService);
    void Clear();
}