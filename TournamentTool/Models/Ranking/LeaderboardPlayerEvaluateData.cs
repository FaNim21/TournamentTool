using System.Text.Json.Serialization;
using TournamentTool.Enums;

namespace TournamentTool.Models.Ranking;

public record LeaderboardTimeline(RunMilestone Milestone, int Time);

public abstract record LeaderboardPlayerEvaluateData(
    Player Player,
    LeaderboardTimeline MainSplit,
    LeaderboardTimeline? PreviousSplit);

public record LeaderboardRankedEvaluateData(
    Player Player,
    int Round,
    LeaderboardTimeline MainSplit,
    LeaderboardTimeline? PreviousSplit) : LeaderboardPlayerEvaluateData(Player, MainSplit, PreviousSplit);
public record LeaderboardPacemanEvaluateData(
    Player Player,
    string WorldID,
    LeaderboardTimeline MainSplit,
    LeaderboardTimeline? PreviousSplit) : LeaderboardPlayerEvaluateData(Player, MainSplit, PreviousSplit);


[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(EntryPacemanMilestoneData), "paceman")]
[JsonDerivedType(typeof(EntryRankedMilestoneData), "ranked")]
public abstract record EntryMilestoneData(
    LeaderboardTimeline Main, 
    LeaderboardTimeline? Previous, 
    int Points);

public record EntryPacemanMilestoneData(
    LeaderboardTimeline Main,
    LeaderboardTimeline? Previous,
    int Points,
    string WorldID) : EntryMilestoneData(Main, Previous, Points);
public record EntryRankedMilestoneData(
    LeaderboardTimeline Main, 
    LeaderboardTimeline? Previous, 
    int Points, 
    int Round) : EntryMilestoneData(Main, Previous, Points);

