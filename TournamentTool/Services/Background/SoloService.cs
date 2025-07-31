using System.IO;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services.Background;

public class SoloService : IBackgroundService
{
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }

    private readonly string _speedrunIGTPath;
    private string _playingWorldName = string.Empty;


    public SoloService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
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
        await Task.Delay(TimeSpan.FromMilliseconds(10_000), token);
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