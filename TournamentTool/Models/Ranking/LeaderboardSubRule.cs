namespace TournamentTool.Models.Ranking;

public class LeaderboardSubRule
{
    public string LuaPath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; //??
    public int Time { get; set; }
    public int BasePoints { get; set; }
    public int MaxWinners { get; set; }
    public bool Repeatable { get; set; }
}