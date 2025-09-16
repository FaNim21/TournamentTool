using System.Security.Cryptography;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Logging;
using TournamentTool.Services.Background;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;

    void EvaluateData(object? data, LeaderboardRuleType ruleType = LeaderboardRuleType.None);
}

public class LeaderboardManager : ILeaderboardManager
{
    private TournamentViewModel Tournament { get; }
    private ILuaScriptsManager LuaManager { get; }
    private ILoggingService Logger { get; }

    public event Action<LeaderboardEntry>? OnEntryUpdate;

    
    public LeaderboardManager(TournamentViewModel tournament, ILuaScriptsManager luaManager, ILoggingService logger)
    {
        Tournament = tournament;
        LuaManager = luaManager;
        Logger = logger;
    }

    public void EvaluateData(object? data, LeaderboardRuleType ruleType = LeaderboardRuleType.None)
    {
        if (data is null) return;

        switch (data)
        {
            case Dictionary<RunMilestone, RankedEvaluateTimelineData> ranked:
                EvaluateTimelines(ranked, ruleType);
                break;
            case LeaderboardPlayerEvaluateData player:
                EvaluatePlayer(player, ruleType);
                break;
        }
    }

    private void EvaluateTimelines(Dictionary<RunMilestone, RankedEvaluateTimelineData> data, LeaderboardRuleType ruleType = LeaderboardRuleType.None)
    {
        if (data.Count == 0) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;

        foreach (var rule in Tournament.Leaderboard.Rules)
        {
            if (!rule.IsEnabled) continue;
            if (ruleType != rule.RuleType && ruleType != LeaderboardRuleType.None) continue;
            if (!data.TryGetValue(rule.ChosenAdvancement, out RankedEvaluateTimelineData? timelineData)) continue;

            foreach (var subRule in rule.SubRules)
            {
                if (timelineData.Evaluations.Count == 0) break;
                List<LeaderboardRankedEvaluateData> subRuleDatas = [];

                int count = 0;
                for (var i = 0; i < timelineData.Evaluations.Count; i++)
                {
                    if (subRule.MaxWinners <= count && subRule.MaxWinners > 0) break;
                    var evaluation = timelineData.Evaluations[i];
                    if (!subRule.EvaluateTime(evaluation.MainSplit.Time)) break;

                    subRuleDatas.Add(evaluation);
                    timelineData.Remove(evaluation);
                    i--;
                    count++;
                }

                if (subRuleDatas.Count == 0) continue;
                UpdateEntries(rule, subRule, subRuleDatas);
            }
        }
    }
    private void EvaluatePlayer(LeaderboardPlayerEvaluateData data, LeaderboardRuleType ruleType = LeaderboardRuleType.None)
    {
        if (data.Player == null) return;
        if (Tournament.Leaderboard.Rules.Count == 0) return;
        
        foreach (var rule in Tournament.Leaderboard.Rules)
        {
            if (!rule.IsEnabled) continue;
            if (ruleType != rule.RuleType && ruleType != LeaderboardRuleType.None) continue;
            
            var subRule = rule.Evaluate(data);
            if (subRule == null) continue;
            
            UpdateEntry(rule, subRule, data);
            break;
        }
    }

    private void UpdateEntries(LeaderboardRule rule, LeaderboardSubRule subRule, List<LeaderboardRankedEvaluateData> datas)
    {
        List<LuaPlayerData> luaPlayerDatas = [];
        foreach (var data in datas)
        {
            if (data.Player == null) continue;
            LeaderboardEntry entry = Tournament.Leaderboard.GetOrCreateEntry(data.Player.UUID);
            LuaPlayerData luaData = new LuaPlayerData(entry, data);
            luaPlayerDatas.Add(luaData);
        }
        
        LuaAPIRankedContext context = new LuaAPIRankedContext(rule, subRule, Tournament, luaPlayerDatas, OnEntryRunRegistered);
        RunScript(subRule.LuaPath, context);
        
        var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Logger.Log($"Evaluated: {luaPlayerDatas.Count} players for {datas[0].MainSplit.Milestone}" +
                   $"for sub rule with desc: {subRule.Description}, so under {subRuleTime}");
    }
    private void UpdateEntry(LeaderboardRule rule, LeaderboardSubRule subRule, LeaderboardPlayerEvaluateData data)
    {
        LeaderboardEntry entry = Tournament.Leaderboard.GetOrCreateEntry(data.Player.UUID);
        LuaAPIContext context = new LuaAPIContext(entry, data, rule, subRule, Tournament, OnEntryRunRegistered);
        
        int oldPosition = entry.Position;
        RunScript(subRule.LuaPath, context);
        
        var playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
        var subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Logger.Log($"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" " +
                   $"in time: {playerTime}, so under {subRuleTime} with new points: {subRule.BasePoints}," +
                   $"advancing from position {oldPosition} to {entry.Position}");
    }
    
    private void OnEntryRunRegistered(LeaderboardEntry entry)
    {
        Tournament.Leaderboard.RecalculateEntryPosition(entry);
        OnEntryUpdate?.Invoke(entry);
        Tournament.PresetIsModified();
    }

    private void RunScript(string path, object context)
    {
        var script = LuaManager.Get(path);
        if (script == null) return;

        try
        {
            script.Run(context);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
}