using System.Text.Json.Serialization;
using MethodTimer;
using TournamentTool.Modules.Logging;

namespace TournamentTool.Models.Ranking;

public class Leaderboard
{
    [JsonIgnore] private Dictionary<string, LeaderboardEntry> _lookupEntries = [];
    public List<LeaderboardEntry> OrderedEntries { get; init; } = [];
    public List<LeaderboardRule> Rules { get; init; } = [];


    public void Initialize()
    {
        _lookupEntries.Clear();
        for (int i = 0; i < OrderedEntries.Count; i++)
        {
            var entry = OrderedEntries[i];
            try
            {
                _lookupEntries.Add(entry.PlayerUUID, entry);
            }
            catch
            {
                LogService.Log($"Cannot add player with UUID {entry.PlayerUUID} because its already in lookup dictionary");
            }
        }
    }
    
    public void AddEntry(LeaderboardEntry entry)
    {
        OrderedEntries.Add(entry);
        try
        {
            _lookupEntries.Add(entry.PlayerUUID, entry);
        }
        catch
        {
            LogService.Log($"Cannot add player with UUID {entry.PlayerUUID} because its already in lookup dictionary");
        }
    }
    public void RemoveEntry(LeaderboardEntry entry)
    {
        OrderedEntries.Remove(entry);
        try
        {
            _lookupEntries.Remove(entry.PlayerUUID);
        }
        catch
        {
            LogService.Log($"Player with UUID {entry.PlayerUUID} doesn't exist in lookup dictionary");
        }
    }

    public void AddRule(LeaderboardRule rule)
    {
        Rules.Add(rule);
    }
    public void RemoveRule(LeaderboardRule rule)
    {
        Rules.Remove(rule);
    }
    
    public void RecalculateEntryPosition(LeaderboardEntry updatedEntry)
    {
        bool wasChanged = false;
        int index = OrderedEntries.IndexOf(updatedEntry);
        if (index == -1) return;
        if (OrderedEntries.Count == 1)
        {
            updatedEntry.Position = 1;
            return;
        }
        
        //Punkty wzrosly
        while (index > 0 && OrderedEntries[index].CompareTo(OrderedEntries[index - 1], Rules[0].ChosenAdvancement) >= 0)
        {
            (OrderedEntries[index - 1], OrderedEntries[index]) = (OrderedEntries[index], OrderedEntries[index - 1]);

            OrderedEntries[index].Position = index + 1;
            OrderedEntries[index - 1].Position = index;
        
            index--;
            wasChanged = true;
        }
        
        //Punkty spadly
        while (index < OrderedEntries.Count - 1 && OrderedEntries[index].CompareTo(OrderedEntries[index + 1], Rules[0].ChosenAdvancement) < 0)
        {
            (OrderedEntries[index + 1], OrderedEntries[index]) = (OrderedEntries[index], OrderedEntries[index + 1]);

            OrderedEntries[index].Position = index + 1;
            OrderedEntries[index + 1].Position = index + 2;
        
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
        Rules.RemoveAt(oldIndex);
        Rules.Insert(newIndex, item);
    }
}