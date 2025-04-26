using TournamentTool.Enums;

namespace TournamentTool.Models.Ranking;

public class LeaderboardPlayerEvaluateData
{
    public Player Player { get; set; } = new();
    public RunMilestone Milestone { get; set; } = RunMilestone.None;
    public int Time { get; set; }
}