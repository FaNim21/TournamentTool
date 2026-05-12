using TournamentTool.Domain.Entities.Ranking;

namespace TournamentTool.Services.Managers.Preset;

public interface ITournamentLeaderboardRepository
{
    IReadOnlyList<LeaderboardEntry> OrderedEntries { get; }
    IReadOnlyList<LeaderboardRule> Rules { get; }

    void AddEntry(LeaderboardEntry entry);
    void RemoveEntry(LeaderboardEntry entry);

    void AddRule(LeaderboardRule rule);
    void RemoveRule(LeaderboardRule rule);

    void RecalculateEntryPosition(LeaderboardEntry updatedEntry);

    LeaderboardEntry GetOrCreateEntry(string uuid);
    LeaderboardEntry? GetEntry(string uuid);

    void MoveRule(int oldIndex, int newIndex);

    void ClearAll();
}