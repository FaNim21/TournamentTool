namespace TournamentTool.Models.Ranking;

public sealed class LeaderboardEntry
{
    public string PlayerUUID { get; init; } = string.Empty;

    public int Points { get; set; }
    public int Position { get; set; } = -1;


    public void AddPoints(int points)
    {
        Points += points;
    }

    public int CompareTo(LeaderboardEntry other)
    {
        int pointComparison = Points.CompareTo(other.Points);
        if (pointComparison != 0) return pointComparison;

        return 0;
    }
}