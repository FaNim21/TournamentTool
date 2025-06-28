using System.IO;
using MoonSharp.Interpreter;
using NuGet.Versioning;

namespace TournamentTool.Models.Ranking;

public class LuaLeaderboardScript
{
    public string FullPath { get; }
    public string Description { get; }
    public NuGetVersion? Version { get; } 

    private Script _script;
    private DynValue _evaluatePlayer;


    public LuaLeaderboardScript(Script script, string fullPath)
    {
        _script = script;
        
        _evaluatePlayer = script.Globals.Get("evaluate_player");
        FullPath = fullPath;
        Description = script.Globals.Get("description")?.String ?? string.Empty;
        Version = GetVersion();
    }
    
    public static LuaLeaderboardScript Load(string name, string path = "")
    {
        string scriptPath = Path.Combine(path, name);
        if (!scriptPath.EndsWith(".lua"))
        {
            scriptPath += ".lua";
        }
        
        var code = File.ReadAllText(scriptPath);
        var script = new Script(CoreModules.Preset_SoftSandbox);
    
        UserData.RegisterType<LuaAPIContext>();
        UserData.RegisterType<LuaAPIRankedContext>();
        UserData.RegisterType<LuaPlayerData>();
        
        script.Globals["print"] = DynValue.NewCallback((context, args) =>
        {
            for (int i = 0; i < args.Count; i++)
            {
                Console.Write(args[i].ToPrintString());
            }
            Console.WriteLine();
            return DynValue.Nil;
        });

        script.DoString(code);

        return new LuaLeaderboardScript(script, scriptPath);
    }
    
    public void Run(object context)
    {
        _script.Globals["api"] = UserData.Create(context);
        _script.Call(_evaluatePlayer, _script.Globals["api"]);
    }
    
    private NuGetVersion? GetVersion()
    {
        var versionString = _script.Globals.Get("version")?.String;
        if (string.IsNullOrWhiteSpace(versionString)) return null;
        
        return NuGetVersion.TryParse(versionString, out var version) ? version : null;
    }
}