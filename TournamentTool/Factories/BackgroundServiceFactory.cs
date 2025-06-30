using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Factories;

public class BackgroundServiceFactory
{
    private readonly TournamentViewModel _tournament;
    private readonly ILeaderboardManager _leaderboard;
    private readonly IPresetSaver _presetSaver;

    
    public BackgroundServiceFactory(TournamentViewModel tournament, ILeaderboardManager leaderboard, IPresetSaver presetSaver)
    {
        _tournament = tournament;
        _leaderboard = leaderboard;
        _presetSaver = presetSaver;
    }

    public IBackgroundService? Create(ControllerMode mode) =>
        mode switch
        {
            ControllerMode.Paceman => new PaceManService(_tournament, _leaderboard, _presetSaver),
            ControllerMode.Ranked => new RankedService(_tournament, _leaderboard),
            ControllerMode.Solo => new SoloService(_tournament, _leaderboard),
            _ => null,
        };
}