using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Services.Logging;

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

public class TournamentLeaderboardRepository : ITournamentLeaderboardRepository, IDisposable
{
    private ILoggingService Logger { get; }
    private readonly ITournamentState _state;

    private Leaderboard Leaderboard => _state.CurrentPreset.Leaderboard;

    private Dictionary<string, LeaderboardEntry> _lookupEntries = [];
    public IReadOnlyList<LeaderboardEntry> OrderedEntries => Leaderboard.OrderedEntries;
    public IReadOnlyList<LeaderboardRule> Rules => Leaderboard.Rules;


    public TournamentLeaderboardRepository(ITournamentState state, ILoggingService logger)
    {
        Logger = logger;
        _state = state;

        _state.PresetChanged += OnPresetChanged;
    }
    public void Dispose()
    {
        _state.PresetChanged -= OnPresetChanged;
    }

    private void OnPresetChanged(object? sender, Tournament? tournament)
    {
        if (tournament == null)
        {
            _lookupEntries.Clear();
            return;
        }

        Initialize();
    }
    private void Initialize()
    {
        _lookupEntries.Clear();
        
        for (int i = 0; i < OrderedEntries.Count; i++)
        {
            var entry = OrderedEntries[i];
            try
            {
                _lookupEntries.Add(entry.PlayerUUID, entry);
            }
            catch (Exception ex)
            {
                Logger.Error($"Cannot add entry with uuid '{entry.PlayerUUID}'\n{ex}");
            }
        }
    }
    
    public void AddEntry(LeaderboardEntry entry)
    {
        Leaderboard.OrderedEntries.Add(entry);
        
        try
        {
            _lookupEntries.Add(entry.PlayerUUID, entry);
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot add entry with uuid '{entry.PlayerUUID}'\n{ex}");
        }
    }
    public void RemoveEntry(LeaderboardEntry entry)
    {
        Leaderboard.OrderedEntries.Remove(entry);
        
        try
        {
            _lookupEntries.Remove(entry.PlayerUUID);
        }
        catch (Exception ex)
        {
            Logger.Error($"Cannot remove entry with uuid '{entry.PlayerUUID}'\n{ex}");
        }
    }

    public void AddRule(LeaderboardRule rule)
    {
        Leaderboard.Rules.Add(rule);
    }
    public void RemoveRule(LeaderboardRule rule)
    {
        Leaderboard.Rules.Remove(rule);
    }
    
    public void RecalculateEntryPosition(LeaderboardEntry updatedEntry)
    {
        bool wasChanged = false;
        int index = Leaderboard.OrderedEntries.IndexOf(updatedEntry);
        if (index == -1) return;
        if (OrderedEntries.Count == 1)
        {
            updatedEntry.Position = 1;
            return;
        }
        
        //Punkty wzrosly
        while (index > 0 && OrderedEntries[index].CompareTo(OrderedEntries[index - 1], Rules[0].ChosenAdvancement) >= 0)
        {
            (Leaderboard.OrderedEntries[index - 1], Leaderboard.OrderedEntries[index]) = (Leaderboard.OrderedEntries[index], Leaderboard.OrderedEntries[index - 1]);

            Leaderboard.OrderedEntries[index].Position = index + 1;
            Leaderboard.OrderedEntries[index - 1].Position = index;
        
            index--;
            wasChanged = true;
        }
        
        //Punkty spadly
        while (index < OrderedEntries.Count - 1 && OrderedEntries[index].CompareTo(OrderedEntries[index + 1], Rules[0].ChosenAdvancement) < 0)
        {
            (Leaderboard.OrderedEntries[index + 1], Leaderboard.OrderedEntries[index]) = (Leaderboard.OrderedEntries[index], Leaderboard.OrderedEntries[index + 1]);

            Leaderboard.OrderedEntries[index].Position = index + 1;
            Leaderboard.OrderedEntries[index + 1].Position = index + 2;
        
            index++;
            wasChanged = true;
        }

        if (wasChanged) return;
        OrderedEntries[index].Position = index + 1;
    }
    
    public LeaderboardEntry GetOrCreateEntry(string uuid)
    {
        LeaderboardEntry? entry = GetEntry(uuid);
        if (entry != null) return entry;
        
        entry = new LeaderboardEntry { PlayerUUID = uuid };
        AddEntry(entry);
        return entry;
    }
    public LeaderboardEntry? GetEntry(string uuid)
    {
        _lookupEntries.TryGetValue(uuid, out var entry);
        return entry;
    }

    public void MoveRule(int oldIndex, int newIndex)
    {
        var item = Rules[oldIndex];
        Leaderboard.Rules.RemoveAt(oldIndex);
        Leaderboard.Rules.Insert(newIndex, item);
    }

    public void ClearAll()
    {
        Leaderboard.OrderedEntries.Clear();
        _lookupEntries.Clear();
        //TODO: 0 zrobic normalne clearowanie wszystkich graczy
    }
}