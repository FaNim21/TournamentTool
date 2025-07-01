using System.IO;
using MoonSharp.Interpreter;
using NuGet.Versioning;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.Modules.Lua;

public enum LuaLeaderboardType
{
    normal,
    ranked
}

public class LuaLeaderboardScript
{
    public string FullPath { get; }
    public LuaLeaderboardType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public NuGetVersion? Version { get; private set; }

    private string _code;
    private Script? _script;
    private DynValue? _evaluatePlayer;
    private bool _isValid;


    public LuaLeaderboardScript(string code, string fullPath)
    {
        _code = code;
        FullPath = fullPath;
    }
    public static LuaLeaderboardScript Load(string name, string path = "")
    {
        string scriptPath = Path.Combine(path, name);
        if (!scriptPath.EndsWith(".lua"))
        {
            scriptPath += ".lua";
        }
        
        var code = File.ReadAllText(scriptPath);
        var script = new LuaLeaderboardScript(code, scriptPath);
        
        var validation = script.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Script validation failed: {validation.SyntaxError?.Message ?? "Unknown error"}");
        }
        
        return script;
    }

    public void Run(object context)
    {
        if (!_isValid || _script == null || _evaluatePlayer == null)
        {
            throw new InvalidOperationException("Script must be validated before running");
        }
        
        _script.Globals["api"] = UserData.Create(context);
        _script.Call(_evaluatePlayer, _script.Globals["api"]);
    }

    public LuaScriptValidationResult Validate(object? testContext = null)
    {
        var result = new LuaScriptValidationResult();

        // Syntax
        var syntaxResult = ValidateSyntax();
        if (!syntaxResult.IsValid)
        {
            result.SyntaxError = syntaxResult.SyntaxError;
            result.IsValid = false;
            return result;
        }

        ExtractMetadata();

        // Runtime
        if (testContext != null)
        {
            var runtimeResult = ValidateRuntime(testContext);
            result.RuntimeErrors.AddRange(runtimeResult.RuntimeErrors);
            result.Warnings.AddRange(runtimeResult.Warnings);
        }
        
        result.IsValid = !result.HasErrors;
        SetValidation(result.IsValid);
        
        return result;
    }
    public LuaScriptValidationResult ValidateSyntax()
    {
        var result = new LuaScriptValidationResult { IsValid = true };
            
        try
        {
            _script = new Script(CoreModules.Preset_SoftSandbox);
            RegisterTypes();
            SetupGlobals();
                
            _script.DoString(_code);
                
            _evaluatePlayer = _script.Globals.Get("evaluate_data");
            if (_evaluatePlayer is not { Type: DataType.Function })
            {
                result.IsValid = false;
                result.SyntaxError = new LuaScriptError
                {
                    Type = "Structure",
                    Message = "Required function 'evaluate_data' not found",
                    FullError = "Script must define: function evaluate_data(api) ... end"
                };
            }
        }
        catch (SyntaxErrorException ex)
        {
            result.IsValid = false;
            result.SyntaxError = ParseLuaError(ex, "Syntax");
        }
        catch (ScriptRuntimeException ex)
        {
            result.IsValid = false;
            result.SyntaxError = ParseLuaError(ex, "Runtime");
        }
            
        return result;
    }
    public LuaScriptValidationResult ValidateRuntime(object testContext)
    {
        var result = new LuaScriptValidationResult { IsValid = true };
        
        if (_script == null || _evaluatePlayer == null)
        {
            result.RuntimeErrors.Add(new LuaScriptError
            {
                Type = "Runtime",
                Message = "Script not properly initialized",
                FullError = "Script must be validated for syntax first"
            });
            return result;
        }
        
        try
        {
            _script.Globals["api"] = UserData.Create(testContext);
            _script.Call(_evaluatePlayer, _script.Globals["api"]);
            
            CheckRequiredGlobals(result);
        }
        catch (ScriptRuntimeException ex)
        {
            result.RuntimeErrors.Add(ParseLuaError(ex, "Runtime"));
        }
        catch (Exception ex)
        {
            result.RuntimeErrors.Add(new LuaScriptError
            {
                Type = "Runtime",
                Message = ex.Message,
                FullError = ex.ToString()
            });
        }
        
        return result;
    }
    
    private void RegisterTypes()
    {
        UserData.RegisterType<LuaAPIContext>();
        UserData.RegisterType<LuaAPIRankedContext>();
        UserData.RegisterType<LuaPlayerData>();
    } 
    private void SetupGlobals()
    {
        _script!.Globals["print"] = DynValue.NewCallback((context, args) =>
        {
            for (int i = 0; i < args.Count; i++)
            {
                Console.Write(args[i].ToPrintString());
            }
            Console.WriteLine();
            return DynValue.Nil;
        });
    }
   
    private void CheckRequiredGlobals(LuaScriptValidationResult result)
    {
        var typeValue = _script!.Globals.Get("type");
        if (typeValue.IsNil() || !Enum.TryParse(typeof(LuaLeaderboardType), typeValue.String, out var _))
        {
            result.Warnings.Add("Script should define 'type' as 'normal' or 'ranked'");
        }
            
        var versionValue = _script.Globals.Get("version");
        if (!versionValue.IsNil() && !NuGetVersion.TryParse(versionValue.String, out _))
        {
            result.Warnings.Add($"Invalid version format: '{versionValue.String}'. Use semantic versioning (e.g., '1.0.0')");
        }
    }
    
    private LuaScriptError ParseLuaError(Exception ex, string errorType)
    {
        var error = new LuaScriptError
        {
            Type = errorType,
            Message = ex.Message,
            FullError = ex.ToString()
        };
            
        var decoratedMessage = ex switch
        {
            ScriptRuntimeException sre => sre.DecoratedMessage,
            SyntaxErrorException se => se.DecoratedMessage,
            _ => ex.Message
        };
            
        var match = RegexPatterns.LuaErrorLogPattern().Match(decoratedMessage);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int lineNumber)) return error;
        error.LineNumber = lineNumber;
        
        var lines = _code.Split('\n');
        if (lineNumber >= 1 && lineNumber <= lines.Length)
        {
            error.LineContent = lines[lineNumber - 1].Trim();
        }

        return error;
    }
    
    public void ExtractMetadata()
    {
        if (_script == null) return;
            
        Description = _script.Globals.Get("description")?.String ?? string.Empty;
            
        var versionString = _script.Globals.Get("version")?.String;
        if (!string.IsNullOrWhiteSpace(versionString) && NuGetVersion.TryParse(versionString, out var version))
        {
            Version = version;
        }
            
        string typeString = _script.Globals.Get("type")?.String ?? "normal";
        if (Enum.TryParse<LuaLeaderboardType>(typeString, true, out var scriptType))
        {
            Type = scriptType;
        }
    }
    public void SetValidation(bool isValid)
    {
        _isValid = isValid;
    }
}