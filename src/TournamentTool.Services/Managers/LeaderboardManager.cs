using TournamentTool.Core.Extensions;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Managers;

public interface ILeaderboardManager
{
    event Action<LeaderboardEntry>? OnEntryUpdate;

    void EvaluateData(object? data, LeaderboardRuleType ruleType = LeaderboardRuleType.None);
}

public class LeaderboardManager : ILeaderboardManager
{
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentLeaderboardRepository _leaderboardRepository;
    private ILuaScriptsManager LuaManager { get; }
    private ILoggingService Logger { get; }

    public event Action<LeaderboardEntry>? OnEntryUpdate;

    
    public LeaderboardManager(ITournamentState tournamentState, ITournamentLeaderboardRepository leaderboardRepository, ILuaScriptsManager luaManager, ILoggingService logger)
    {
        _tournamentState = tournamentState;
        _leaderboardRepository = leaderboardRepository;
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
        if (_leaderboardRepository.Rules.Count == 0) return;

        foreach (LeaderboardRule rule in _leaderboardRepository.Rules)
        {
            if (!rule.IsEnabled) continue;
            if (ruleType != rule.RuleType && ruleType != LeaderboardRuleType.None) continue;
            if (!data.TryGetValue(rule.ChosenAdvancement, out RankedEvaluateTimelineData? timelineData)) continue;

            foreach (LeaderboardSubRule subRule in rule.SubRules)
            {
                if (timelineData.Evaluations.Count == 0) break;
                List<LeaderboardRankedEvaluateData> subRuleDatas = [];

                int count = 0;
                for (int i = 0; i < timelineData.Evaluations.Count; i++)
                {
                    if (subRule.MaxWinners <= count && subRule.MaxWinners > 0) break;
                    
                    LeaderboardRankedEvaluateData evaluation = timelineData.Evaluations[i];
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
        if (_leaderboardRepository.Rules.Count == 0) return;
        
        foreach (LeaderboardRule rule in _leaderboardRepository.Rules)
        {
            if (!rule.IsEnabled) continue;
            if (ruleType != rule.RuleType && ruleType != LeaderboardRuleType.None) continue;

            LeaderboardSubRule? subRule = rule.Evaluate(data);
            if (subRule is null) continue;
            
            UpdateEntry(rule, subRule, data);
            break;
        }
    }

    private void UpdateEntries(LeaderboardRule rule, LeaderboardSubRule subRule, List<LeaderboardRankedEvaluateData> datas)
    {
        List<LuaPlayerData> luaPlayerDatas = [];
        foreach (LeaderboardRankedEvaluateData data in datas)
        {
            if (data.Player == null) continue;
            LeaderboardEntry entry = _leaderboardRepository.GetOrCreateEntry(data.Player.UUID);
            LuaPlayerData luaData = new LuaPlayerData(entry, data);
            luaPlayerDatas.Add(luaData);
        }
        
        LuaAPIRankedContext context = new LuaAPIRankedContext(rule, subRule, _tournamentState.CurrentPreset, luaPlayerDatas, OnEntryRunRegistered);
        RunScript(subRule.LuaPath, context);
        
        string subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Logger.Log($"Evaluated: {luaPlayerDatas.Count} players for {datas[0].MainSplit.Milestone}" +
                   $"for sub rule with desc: {subRule.Description}, so under {subRuleTime}");
    }
    private void UpdateEntry(LeaderboardRule rule, LeaderboardSubRule subRule, LeaderboardPlayerEvaluateData data)
    {
        LeaderboardEntry entry = _leaderboardRepository.GetOrCreateEntry(data.Player.UUID);
        LuaAPIContext context = new LuaAPIContext(entry, data, rule, subRule, _tournamentState.CurrentPreset, OnEntryRunRegistered);
        
        int oldPosition = entry.Position;
        RunScript(subRule.LuaPath, context);

        string playerTime = TimeSpan.FromMilliseconds(data.MainSplit.Time).ToFormattedTime();
        string subRuleTime = TimeSpan.FromMilliseconds(subRule.Time).ToFormattedTime();
        Logger.Log($"Player: \"{data.Player.InGameName}\" just achieved milestone: \"{data.MainSplit.Milestone}\" " +
                   $"in time: {playerTime}, so under {subRuleTime} with new points: {subRule.BasePoints}, " +
                   $"advancing from position {oldPosition} to {entry.Position}");
    }
    
    private void OnEntryRunRegistered(LeaderboardEntry entry)
    {
        //TODO: 0 Leaderboard binding update
        _leaderboardRepository.RecalculateEntryPosition(entry);
        OnEntryUpdate?.Invoke(entry);
        _tournamentState.MarkAsModified();
    }

    private void RunScript(string path, object context)
    {
        LuaLeaderboardScript? script = LuaManager.Get(path);
        if (script is null) return;

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