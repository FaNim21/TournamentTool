using MoonSharp.Interpreter;
using TournamentTool.Factories;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models.Ranking;

public abstract class LuaAPIBase
{
    protected readonly Action<LeaderboardEntry> _onEntryRunRegistered;
    protected readonly TournamentViewModel _tournament;
    protected readonly LeaderboardSubRule _subRule;
    public int BasePoints => _subRule.BasePoints;


    protected LuaAPIBase(TournamentViewModel tournament, LeaderboardSubRule subRule, Action<LeaderboardEntry> onEntryRunRegistered)
    {
        _subRule = subRule;
        _tournament = tournament;
        _onEntryRunRegistered = onEntryRunRegistered;
    }
}

public class LuaAPIContext : LuaAPIBase
{
    private readonly LeaderboardPlayerEvaluateData _data;
    private readonly LeaderboardEntry _entry;
    
    public int PlayerPosition => _entry.Position;
    public int PlayerPoints => _entry.Points;
    public int PlayerTime => _data.MainSplit.Time;
    public int PlayerMilestoneBestTime => _entry.GetBestMilestoneTime(_data.MainSplit.Milestone);
    public string PlayerName => _data.Player.InGameName!;


    public LuaAPIContext(LeaderboardEntry entry, 
        LeaderboardPlayerEvaluateData data, 
        LeaderboardSubRule subRule, 
        TournamentViewModel tournament, 
        Action<LeaderboardEntry> onEntryRunRegistered) : base(tournament, subRule, onEntryRunRegistered)
    {
        _entry = entry;
        _data = data;
    }

    public void RegisterMilestone(int points)
    {
        var milestone = LeaderboardEntryMilestoneFactory.Create(_data, points);
        var success = _entry.AddMilestone(milestone);
        if (!success) return;

        _onEntryRunRegistered.Invoke(_entry);
    }
}

public class LuaPlayerData
{
    [MoonSharpHidden] public readonly LeaderboardEntry entry;
    [MoonSharpHidden] public readonly LeaderboardPlayerEvaluateData data;

    public int Position => entry.Position;
    public int Points => entry.Points;
    public int Time => data.MainSplit.Time;
    public string Name => data.Player.InGameName!;
    
    
    public LuaPlayerData(LeaderboardEntry entry, LeaderboardPlayerEvaluateData data)
    {
        this.data = data;
        this.entry = entry;
    }
}

public class LuaAPIRankedContext : LuaAPIBase
{
    public List<LuaPlayerData> Players { get; }
    public int Round => _tournament.ManagementData is RankedManagementData ranked ? ranked.Rounds : 1;
    public int PlayersInRound => _tournament.ManagementData is RankedManagementData ranked ? ranked.Players : 1;
    public int CompletionsInRound => _tournament.ManagementData is RankedManagementData ranked ? ranked.Completions : 1;
    
    
    public LuaAPIRankedContext( LeaderboardSubRule subRule, 
        TournamentViewModel tournament, 
        List<LuaPlayerData> players,
        Action<LeaderboardEntry> onEntryRunRegistered) : base(tournament, subRule, onEntryRunRegistered)
    {
        Players = players;
    }

    public void RegisterMilestone(LuaPlayerData player, int points)
    {
        var milestone = LeaderboardEntryMilestoneFactory.Create(player.data, points);
        var success = player.entry.AddMilestone(milestone);
        if (!success) return;

        _onEntryRunRegistered.Invoke(player.entry);
    }
}