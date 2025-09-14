using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Modules.Logging;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Factories;

public class BackgroundServiceFactory
{
    private readonly TournamentViewModel _tournament;
    private readonly ILeaderboardManager _leaderboard;
    private readonly IPresetSaver _presetSaver;
    private readonly ILoggingService _logger;


    public BackgroundServiceFactory(TournamentViewModel tournament, ILeaderboardManager leaderboard, IPresetSaver presetSaver, ILoggingService logger)
    {
        _tournament = tournament;
        _leaderboard = leaderboard;
        _presetSaver = presetSaver;
        _logger = logger;
    }

    public IBackgroundService? Create(ControllerMode mode) =>
        mode switch
        {
            ControllerMode.Paceman => new PaceManService(_tournament, _leaderboard, _presetSaver),
            ControllerMode.Ranked => new RankedService(_tournament, _leaderboard, _logger),
            ControllerMode.Solo => new SoloService(_tournament, _leaderboard),
            _ => null,
        };
}