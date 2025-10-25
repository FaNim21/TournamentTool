namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentPresetManager
{
    public ITournamentState State { get; }
    public ITournamentPlayerRepository PlayerRepository { get; }
    public ITournamentLeaderboardRepository LeaderboardRepository { get; }
}