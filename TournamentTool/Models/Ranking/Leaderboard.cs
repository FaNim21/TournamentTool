using MethodTimer;

namespace TournamentTool.Models.Ranking;

public record Leaderboard
{
    public List<LeaderboardEntry> Entries { get; init; } = [];
    public List<LeaderboardRule> Rules { get; init; } = [];

    
    [Time]
    public void RecalculateEntryPosition(LeaderboardEntry updatedEntry)
    {
        int index = Entries.IndexOf(updatedEntry);
        if (index == -1) return;
        if (Entries.Count == 1)
        {
            updatedEntry.Position = 1;
            return;
        }
            
        //Punkty wzrosly
        while (index > 0 && Entries[index].CompareTo(Entries[index - 1], Rules[0].ChosenAdvancement) >= 0)
        {
            (Entries[index - 1], Entries[index]) = (Entries[index], Entries[index - 1]);

            Entries[index].Position = index + 1;
            Entries[index - 1].Position = index;
        
            index--;
        }
        
        //Punkty spadly
        while (index < Entries.Count - 1 && Entries[index].CompareTo(Entries[index + 1], Rules[0].ChosenAdvancement) < 0)
        {
            (Entries[index + 1], Entries[index]) = (Entries[index], Entries[index + 1]);

            Entries[index].Position = index + 1;
            Entries[index + 1].Position = index + 2;
        
            index++;
        }
    }
    
    public LeaderboardEntry GetOrCreateEntry(string uuid)
    {
        LeaderboardEntry? entry = GetEntry(uuid);
        if (entry != null) return entry;
        
        entry = new LeaderboardEntry { PlayerUUID = uuid };
        Entries.Add(entry);
        return entry;
    }
    public LeaderboardEntry? GetEntry(string uuid)
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (entry.PlayerUUID.Equals(uuid)) return entry;
        }
        return null;
    }
}