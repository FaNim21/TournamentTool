using TournamentTool.Enums;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models.Ranking;

public class LeaderboardPlayerEvaluateData
{
    public PlayerViewModel PlayerViewModel { get; set; } = new();
    public RunMilestone Milestone { get; set; } = RunMilestone.None;
    public int Time { get; set; }
}