using TournamentTool.Services.Logging.Profiling;

namespace TournamentTool.Services.Background;

public interface IBackgroundService
{
    int DelayMiliseconds { get; }

    void RegisterData(IBackgroundDataReceiver? receiver);
    void UnregisterData(IBackgroundDataReceiver? receiver);
    Task Update(CancellationToken token);
}