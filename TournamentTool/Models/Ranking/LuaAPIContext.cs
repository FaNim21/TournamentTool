using System.ComponentModel.DataAnnotations;
using MoonSharp.Interpreter;
using TournamentTool.Enums;
using TournamentTool.Factories;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models.Ranking;

public abstract class LuaAPIBase
{
    [MoonSharpHidden] protected readonly Action<LeaderboardEntry>? _onEntryRunRegistered;
    [MoonSharpHidden] protected readonly TournamentViewModel? _tournament;
    [MoonSharpHidden] protected readonly LeaderboardRule _rule;
    [MoonSharpHidden] protected readonly LeaderboardSubRule _subRule;
    
    public string RuleName => _rule.Name;
    public string RuleType => _rule.RuleType.ToString().ToLower();
    public string MilestoneName
    {
        get
        {
            DisplayAttribute? display = _rule.ChosenAdvancement.GetDisplay();
            string output = _rule.ChosenAdvancement.ToString();
            if (display != null && !string.IsNullOrEmpty(display.ShortName))
            {
                output = display.ShortName;
            }
            return output;
        }
    }

    public string Description => _subRule.Description;
    public int TimeThreshold => _subRule.Time;
    public int BasePoints => _subRule.BasePoints;


    protected LuaAPIBase(TournamentViewModel tournament, 
        LeaderboardRule rule, 
        LeaderboardSubRule subRule,
        Action<LeaderboardEntry>? onEntryRunRegistered)
    {
        _rule = rule;
        _subRule = subRule;
        _tournament = tournament;
        _onEntryRunRegistered = onEntryRunRegistered;
    }

    public object? GetVariable(string name) => _subRule.GetVariable(name);
    public void SetVariable(string name, object value) => _subRule.SetVariable(name, value);
}

public class LuaAPIContext : LuaAPIBase
{
    private readonly LeaderboardPlayerEvaluateData _data;
    private readonly LeaderboardEntry _entry;
    
    public int PlayerPosition => _entry.Position;
    public int PlayerPoints => _entry.Points;
    public int PlayerTime => _data.MainSplit.Time;
    public int PlayerMilestoneBestTime => _entry.GetBestMilestoneTime(_data.MainSplit.Milestone);
    public int PlayerMilestoneAmount => _entry.GetBestMilestoneAmount(_data.MainSplit.Milestone);
    public int PlayerMilestoneTotalTime => _entry.GetBestMilestoneTotalTime(_data.MainSplit.Milestone);
    public string PlayerName => _data.Player.InGameName!;


    public LuaAPIContext(LeaderboardEntry entry, 
        LeaderboardPlayerEvaluateData data, 
        LeaderboardRule rule, 
        LeaderboardSubRule subRule, 
        TournamentViewModel tournament, 
        Action<LeaderboardEntry>? onEntryRunRegistered) : base(tournament, rule, subRule, onEntryRunRegistered)
    {
        _entry = entry;
        _data = data;
    }

    public void RegisterMilestone(int points)
    {
        var milestone = LeaderboardEntryMilestoneFactory.Create(_data, points);
        var success = _entry.AddMilestone(milestone);
        if (!success) return;

        _onEntryRunRegistered?.Invoke(_entry);
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
    public int MilestoneBestTime => entry.GetBestMilestoneTime(data.MainSplit.Milestone);
    public int MilestoneAmount => entry.GetBestMilestoneAmount(data.MainSplit.Milestone);
    public int MilestoneTotalTime => entry.GetBestMilestoneTotalTime(data.MainSplit.Milestone);
    
    
    public LuaPlayerData(LeaderboardEntry entry, LeaderboardPlayerEvaluateData data)
    {
        this.data = data;
        this.entry = entry;
    }
}
public class LuaAPIRankedContext : LuaAPIBase
{
    public List<LuaPlayerData> Players { get; }
    public int Round
    {
        get
        {
            if (_tournament == null) return 1;
            var player = Players[0];
            if (player is { data: LeaderboardRankedEvaluateData rankedData })
            {
                return rankedData.Round;
            }
            return _tournament.ManagementData is RankedManagementData ranked ? ranked.Rounds : 1;
        }
    }
    public int PlayersInRound
    {
        get
        {
            if (_tournament == null) return 1;
            return _tournament.ManagementData is RankedManagementData ranked ? ranked.Players : 1;
        }
    }
    public int CompletionsInRound
    {
        get
        {
            if (_tournament == null) return 1;
            return _tournament.ManagementData is RankedManagementData ranked ? ranked.Completions : 1;
        }
    }
    public int MaxWinners => _subRule.MaxWinners;


    public LuaAPIRankedContext(LeaderboardRule rule,
        LeaderboardSubRule subRule, 
        TournamentViewModel tournament, 
        List<LuaPlayerData> players,
        Action<LeaderboardEntry>? onEntryRunRegistered) : base(tournament, rule, subRule, onEntryRunRegistered)
    {
        Players = players;
    }

    public void RegisterMilestone(LuaPlayerData player, int points)
    {
        var milestone = LeaderboardEntryMilestoneFactory.Create(player.data, points);
        var success = player.entry.AddMilestone(milestone);
        if (!success) return;

        _onEntryRunRegistered?.Invoke(player.entry);
    }
}