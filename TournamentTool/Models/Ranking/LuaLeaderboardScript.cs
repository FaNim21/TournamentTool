using System.IO;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;
using NuGet.Versioning;

namespace TournamentTool.Models.Ranking;

public enum LuaLeaderboardType
{
    normal,
    ranked
}

public class LuaLeaderboardScript
{
    public string FullPath { get; }
    public LuaLeaderboardType Type { get; }
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

        string type = script.Globals.Get("type")?.String ?? "Normal";
        if (!Enum.TryParse(typeof(LuaLeaderboardType), type, true, out var scriptType)) return;
        Type = (LuaLeaderboardType)scriptType;
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

        try
        {
            script.DoString(code);
        }
        catch (ScriptRuntimeException ex)
        {
            //blad wykonania skryptu
            var line = ex.DecoratedMessage;
            var match = Regex.Match(line, @"chunk_0:\((\d+),");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
            {
                var luaLines = code.Split('\n');
                if (lineNumber >= 1 && lineNumber <= luaLines.Length)
                {
                    Console.WriteLine($"Runtime error at line {lineNumber}: {ex.Message}");
                    Console.WriteLine($"\"{luaLines[lineNumber - 1].Trim()}\"");
                }
            }
        }
        catch (SyntaxErrorException ex)
        {
            //blad ze skladnia skryptu
            var line = ex.DecoratedMessage;
            var match = Regex.Match(line, @"chunk_0:\((\d+),");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int lineNumber))
            {
                var luaLines = code.Split('\n');
                if (lineNumber >= 1 && lineNumber <= luaLines.Length)
                {
                    Console.WriteLine($"Syntax error at line {lineNumber}: {ex.Message}");
                    Console.WriteLine($"\"{luaLines[lineNumber - 1].Trim()}\"");
                }
            }
        }

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