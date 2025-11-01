using TournamentTool.Core.Utils;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Background;

public class SoloService : IBackgroundService
{
    private ITournamentPresetManager Tournament { get; }
    private ILeaderboardManager Leaderboard { get; }
    
    public int DelayMiliseconds => 10_000;

    private readonly string _speedrunIGTPath;
    private string _playingWorldName = string.Empty;


    public SoloService(ITournamentPresetManager tournament, ILeaderboardManager leaderboard)
    {
        Tournament = tournament;
        Leaderboard = leaderboard;
        
        _speedrunIGTPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "speedrunigt");
    }
    
    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        
    }
    public void UnregisterData(IBackgroundDataReceiver? receiver)
    {
        
    }

    public async Task Update(CancellationToken token)
    {
        if (string.IsNullOrEmpty(_playingWorldName))
        {
            
        }
        
        await LoadSpeedrunIGTData();
    }

    private async Task LoadSpeedrunIGTData()
    {
        try
        {
            await JsonFileHelper.LoadAsync<object>("");
        }
        catch { /**/ }
    }
}