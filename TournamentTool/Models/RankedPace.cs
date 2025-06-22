namespace TournamentTool.Models;


public class RankedPace
{
    public string UUID { get; set; } = string.Empty;
    public string InGameName { get; set; } = string.Empty;
    public int EloRate { get; set; } = -1;
    public List<string> Timelines { get; set; } = [];
}