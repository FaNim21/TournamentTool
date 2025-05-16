using TournamentTool.Interfaces;

namespace TournamentTool.Services;

public class RankedService : IBackgroundService
{
    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        
    }

    public void UnregisterData(IBackgroundDataReceiver? receiver)
    {
        
    }

    public async Task Update(CancellationToken token)
    {
        await Task.Delay(1000, token);
    }
}