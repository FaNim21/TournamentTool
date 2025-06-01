using TournamentTool.Enums;

namespace TournamentTool.Models.Ranking;

public record LeaderboardTimeline(RunMilestone Milestone, int Time);

public record LeaderboardPlayerEvaluateData(
    Player Player,
    LeaderboardTimeline MainSplit,
    LeaderboardTimeline? PreviousSplit);
public record LeaderboardPacemanEvaluateData(
    Player Player,
    string WorldID,
    LeaderboardTimeline MainSplit,
    LeaderboardTimeline? PreviousSplit)
    : LeaderboardPlayerEvaluateData(Player, MainSplit, PreviousSplit);
