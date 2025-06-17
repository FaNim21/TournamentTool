using System.IO;
using MoonSharp.Interpreter;
using TournamentTool.Factories;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;

    void EvaluatePlayer(LeaderboardPlayerEvaluateData data);
}

public class LeaderboardManager : ILeaderboardManager
{
    private TournamentViewModel Tournament { get; }
    private ILuaScriptsManager LuaManager { get; }

    public event Action<LeaderboardEntry>? OnEntryUpdate;

    
    public LeaderboardManager(TournamentViewModel tournament, ILuaScriptsManager luaManager)
    {
        Tournament = tournament;
        LuaManager = luaManager;
    }
    
    public void EvaluatePlayer(LeaderboardPlayerEvaluateData data)
    {
        if (data.Player == null) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;
        
        foreach (var rule in Tournament.Leaderboard.Rules)
        {
            var subRule = rule.Evaluate(data);
            if (subRule == null) continue;
            
            UpdateEntry(subRule, data);
            break;
        }
    }

    private void UpdateEntry(LeaderboardSubRule subRule, LeaderboardPlayerEvaluateData data)
    {
        LeaderboardEntry entry = Tournament.Leaderboard.GetOrCreateEntry(data.Player.UUID);
        LuaAPIContext context = new LuaAPIContext(entry, data, subRule, OnEntryRunRegistered);
        
        int oldPosition = entry.Position;
        var script = LuaManager.GetOrLoad(subRule.LuaPath);
        if (script == null) return;
        
        script.Run(context);
        
        var playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
        var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Console.WriteLine($"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" in time: {playerTime}, so under {subRuleTime} with new points: {subRule.BasePoints}, advancing from position {oldPosition} to {entry.Position}");
    }
    private void OnEntryRunRegistered(LeaderboardEntry entry)
    {
        Tournament.Leaderboard.RecalculateEntryPosition(entry);
        OnEntryUpdate?.Invoke(entry);
        Tournament.PresetIsModified();
    }
}