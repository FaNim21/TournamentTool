using System.IO;
using NuGet.Versioning;
using TournamentTool.Enums;
using TournamentTool.Modules.Lua;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using ZLinq;

namespace TournamentTool.Managers;

public record LuaLeaderboardScriptEntry(string Name, string FullPath, string Description, string Version, LuaLeaderboardType Type);

public interface ILuaScriptsManager
{
    LuaLeaderboardScript AddOrReload(string name);
    LuaLeaderboardScript? Get(string name);
    IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList();
}

public class LuaScriptsManager : BaseViewModel, ILuaScriptsManager
{
    private readonly TournamentViewModel _tournament;
    
    private readonly Dictionary<string, LuaLeaderboardScript> _leaderboardScripts = [];

    
    public LuaScriptsManager(TournamentViewModel tournament)
    {
        _tournament = tournament;
        
        LoadLuaScripts();
        CheckDefaultScriptsForUpdate();
    }

    private void LoadLuaScripts()
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
            catch { /**/ }
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
                Console.WriteLine($"Failed to update default script: {name} - {ex.Message}");
            }
        }
    }

    public LuaLeaderboardScript AddOrReload(string name)
    {
        var loaded = LuaLeaderboardScript.Load(name, Consts.LeaderboardScriptsPath);
        _leaderboardScripts[name] = loaded;
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
                kvp.Value.Type))
            .ToList();
    }
}

public static class LuaDefaultScripts
{
    public record DefaultScript(NuGetVersion Version, string Code);

    public static readonly Dictionary<string, DefaultScript> DefaultScripts = new()
    {
        ["normal_add_base_points"] = new DefaultScript(NuGetVersion.Parse("0.2.0"),
            """
            version = "0.2.0"
            type = "normal"
            description = "Basic script adding base point to successfully evaluated player"

            function evaluate_data(api)
                api:register_milestone(api.base_points)
            end
            """),
        ["ranked_add_base_points"] = new DefaultScript(NuGetVersion.Parse("0.2.0"),
            """
            version = "0.2.0"
            type = "ranked"
            description = "Basic Ranked type script adding base point to all evaluated players"

            function evaluate_data(api)
                for _, player in ipairs(api.players) do
                    api:register_milestone(player, api.base_points)
                end
            end
            """),
    };

}