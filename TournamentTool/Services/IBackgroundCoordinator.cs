using TournamentTool.Interfaces;

namespace TournamentTool.Services;

public interface IBackgroundCoordinator
{
    void Register(IBackgroundDataReceiver? receiver);
    void Unregister(IBackgroundDataReceiver? receiver);
    void Initialize(IBackgroundService backgroundService);
    void Clear();
}