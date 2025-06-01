namespace TournamentTool.Models.Ranking;

public record Leaderboard
{
    public List<LeaderboardEntry> Entries { get; init; } = [];
    public List<LeaderboardRule> Rules { get; init; } = [];


    public LeaderboardEntry? GetEntry(string uuid)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.PlayerUUID.Equals(uuid)) return entry;
        }
        return null;
    }
}