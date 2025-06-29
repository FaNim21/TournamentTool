using System.IO;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using ZLinq;

namespace TournamentTool.Managers;

public record LuaLeaderboardScriptEntry(string Name, string FullPath, string Description, string Version, LuaLeaderboardType Type);

public interface ILuaScriptsManager
{
    void LoadLuaScripts();
    void AddOrReload(string name);
    LuaLeaderboardScript? GetOrLoad(string name);
    IReadOnlyList<LuaLeaderboardScriptEntry> GetScriptsList();
}

public class LuaScriptsManager : BaseViewModel, ILuaScriptsManager
{
    private readonly TournamentViewModel _tournament;
    
    private readonly Dictionary<string, LuaLeaderboardScript> _scripts = [];

    
    public LuaScriptsManager(TournamentViewModel tournament)
    {
        _tournament = tournament;
        
        LoadLuaScripts();
    }
    
    public void LoadLuaScripts()
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
            // Console.WriteLine(ex);
        }
    }

    public LuaLeaderboardScript? GetOrLoad(string name)
    {
        if (_scripts.TryGetValue(name, out var cached)) return cached;

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
        LuaLeaderboardType type = _tournament.ControllerMode == ControllerMode.Ranked ? LuaLeaderboardType.ranked : LuaLeaderboardType.normal;
        
        return _scripts
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