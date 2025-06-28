using System.IO;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Managers;

public record LuaLeaderboardScriptEntry(string Name, string FullPath, string Description, string Version);

public interface ILuaScriptsManager
{
    void AddOrReload(string name);
    LuaLeaderboardScript? GetOrLoad(string name);
    IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList();
}

public class LuaScriptsManager : BaseViewModel, ILuaScriptsManager
{
    private readonly Dictionary<string, LuaLeaderboardScript> _scripts = [];

    
    public LuaScriptsManager()
    {
        CacheLuaScripts();
    }
    
    private void CacheLuaScripts()
    {
        var scripts = Directory.GetFiles(Consts.LeaderboardScriptsPath, "*.lua", SearchOption.TopDirectoryOnly).AsSpan();

        for (int i = 0; i < scripts.Length; i++)
        {
            var script = scripts[i];
            var name = Path.GetFileNameWithoutExtension(script);
            
            AddOrReload(name);
        }
    }

    public void AddOrReload(string name)
    {
        try
        {
            var loaded = LuaLeaderboardScript.Load(name, Consts.LeaderboardScriptsPath);
            _scripts[name] = loaded;
        }
        catch (Exception ex)
        {
            //TODO: 1 to jest dobre pod wyswietlanie bledow w lua do debugowania skryptow dla ludzi (wiadomo tylko syntax rzeczy da rade sprawdzic)
            Console.WriteLine(ex.Message);
        }
    }

    public LuaLeaderboardScript? GetOrLoad(string name)
    {
        if (_scripts.TryGetValue(name, out var cached))
            return cached;

        try
        {
            var loaded = LuaLeaderboardScript.Load(name, Consts.LeaderboardScriptsPath);
            _scripts[name] = loaded;
            return loaded;
        }
        catch { /**/ }

        return null;
    }

    public IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList()
    {
        return _scripts.Select(kvp => new LuaLeaderboardScriptEntry(kvp.Key, kvp.Value.FullPath, kvp.Value.Description, kvp.Value.Version?.ToNormalizedString() ?? "Unknown" )).ToList();
    }
}