namespace TournamentTool.Modules.Lua;

public class LuaScriptValidationResult
{
    public bool IsValid { get; set; }
    public LuaScriptError? SyntaxError { get; set; }
    public List<LuaScriptError> RuntimeErrors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    
    public bool HasErrors => SyntaxError != null || RuntimeErrors.Count != 0;
}

public class LuaScriptError
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? LineNumber { get; set; }
    public string? LineContent { get; set; }
    public string FullError { get; set; } = string.Empty;
}
