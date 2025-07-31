using TournamentTool.Enums;
using TournamentTool.Interfaces;

namespace TournamentTool.Services.Background;

public interface IBackgroundService
{
    void RegisterData(IBackgroundDataReceiver? receiver);
    void UnregisterData(IBackgroundDataReceiver? receiver);
    Task Update(CancellationToken token);
}