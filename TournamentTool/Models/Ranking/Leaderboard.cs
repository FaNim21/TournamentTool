namespace TournamentTool.Models.Ranking;

public sealed class Leaderboard
{
    public List<LeaderboardEntry> Entries { get; set; } = [];
    public List<LeaderboardRule> Rules { get; set; } = [];
}