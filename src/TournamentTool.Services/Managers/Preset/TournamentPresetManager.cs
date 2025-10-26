using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Managers.Preset;

public class TournamentPresetManager : ITournamentPresetManager
{
    public ITournamentState State { get; }
    public ITournamentPlayerRepository PlayerRepository { get; }
    public ITournamentLeaderboardRepository LeaderboardRepository { get; }

    public Tournament CurrentPreset => State.CurrentPreset;
    public bool IsModified => State.IsModified;

    public string Name => State.CurrentPreset.Name;
    public bool IsPresetModified => State.IsModified;


    public TournamentPresetManager(ITournamentState state, ITournamentPlayerRepository playerRepository, ITournamentLeaderboardRepository leaderboardRepository)
    {
        State = state;
        PlayerRepository = playerRepository;
        LeaderboardRepository = leaderboardRepository;
    }

    public void MarkAsModified() => State.MarkAsModified();
    public void MarkAsUnmodified() => State.MarkAsUnmodified();
}