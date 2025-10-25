namespace TournamentTool.Domain.Entities.Ranking;

public class Leaderboard
{
    public List<LeaderboardEntry> OrderedEntries { get; init; } = [];
    public List<LeaderboardRule> Rules { get; init; } = [];
}