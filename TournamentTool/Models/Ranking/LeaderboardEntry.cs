namespace TournamentTool.Models.Ranking;

public sealed class LeaderboardEntry
{
    public string PlayerUUID { get; init; } = string.Empty;

    public int Points { get; set; }


    public void AddPoints(int points)
    {
        Points += points;
    }
}