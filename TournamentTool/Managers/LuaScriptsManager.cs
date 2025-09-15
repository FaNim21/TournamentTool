using System.IO;
using TournamentTool.Enums;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.Lua;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;
using ZLinq;

namespace TournamentTool.Managers;

public record LuaLeaderboardScriptEntry(string Name, string FullPath, string Description, string Version, LuaLeaderboardType Type, IReadOnlyList<LuaCustomVariable> CustomVariables);

public interface ILuaScriptsProvider
{
    LuaLeaderboardScript? Get(string name);
}

public interface ILuaScriptsManager : ILuaScriptsProvider
{
    void LoadLuaScripts();
    LuaLeaderboardScript AddOrReload(string name);
    IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList();
}

public class LuaScriptsManager : ILuaScriptsManager
{
    private ILoggingService Logger { get; }
    private readonly TournamentViewModel _tournament;
    
    private readonly Dictionary<string, LuaLeaderboardScript> _leaderboardScripts = [];

    
    public LuaScriptsManager(TournamentViewModel tournament, ILoggingService logger)
    {
        Logger = logger;
        _tournament = tournament;
        
        CheckDefaultScriptsForUpdate();
    }

    public void LoadLuaScripts()
    {
        var scripts = Directory.GetFiles(Consts.LeaderboardScriptsPath, "*.lua", SearchOption.TopDirectoryOnly).AsSpan();

        for (int i = 0; i < scripts.Length; i++)
        {
            var script = scripts[i];
            var name = Path.GetFileNameWithoutExtension(script);

            try
            {
                AddOrReload(name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
    private void CheckDefaultScriptsForUpdate()
    {
        foreach (var (name, script) in LuaDefaultScripts.DefaultScripts)
        {
            try
            {
                bool shouldUpdate;
                string path = Path.Combine(Consts.LeaderboardScriptsPath, $"{name}.lua");

                if (!_leaderboardScripts.TryGetValue(name, out var loadedScript))
                {
                    shouldUpdate = loadedScript?.Version == null || loadedScript.Version < script.Version;
                }
                else
                {
                    shouldUpdate = true;
                }

                if (!shouldUpdate) continue;
                File.WriteAllText(path, script.Code);
                AddOrReload(name);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update default script: {name} - {ex.Message}");
            }
        }
    }

    public LuaLeaderboardScript AddOrReload(string name)
    {
        LuaLeaderboardScript loaded = LuaLeaderboardScript.Load(name, Consts.LeaderboardScriptsPath);
        _leaderboardScripts[name] = loaded;

        if (loaded.CustomVariables.Count == 0) return loaded;
        for (int i = 0; i < _tournament.Leaderboard.Rules.Count; i++)
        {
            var rule = _tournament.Leaderboard.Rules[i];
            for (int j = 0; j < rule.SubRules.Count; j++)
            {
                var subRule = rule.SubRules[j];
                if (!subRule.LuaPath.Equals(name)) continue;
                
                subRule.UpdateCustomVariables(loaded.CustomVariables);
            }
        }
        
        return loaded;
    }
    public LuaLeaderboardScript? Get(string name)
    {
        _leaderboardScripts.TryGetValue(name, out var cached);
        return cached;
    }
    public IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList()
    {
        LuaLeaderboardType type = _tournament.ControllerMode == ControllerMode.Ranked ? LuaLeaderboardType.ranked : LuaLeaderboardType.normal;
        
        return _leaderboardScripts
            .AsValueEnumerable()
            .Where(kvp => kvp.Value.Type == type)
            .Select(kvp => new LuaLeaderboardScriptEntry(
                kvp.Key, 
                kvp.Value.FullPath, 
                kvp.Value.Description, 
                kvp.Value.Version?.ToNormalizedString() ?? "Unknown", 
                kvp.Value.Type, 
                kvp.Value.CustomVariables))
            .ToList();
    }
}