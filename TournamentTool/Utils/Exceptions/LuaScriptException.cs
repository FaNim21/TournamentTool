using System.Text;
using TournamentTool.Modules.Lua;

namespace TournamentTool.Utils.Exceptions;

public class LuaScriptException : Exception
{
    public string Type { get; }
    public int? LineNumber { get; }
    public string? LineContent { get; }
    public string FullError { get; }

    public LuaScriptException(LuaScriptError error) : base(error.Message)
    {
        Type = error.Type;
        LineNumber = error.LineNumber;
        LineContent = error.LineContent;
        FullError = error.FullError;
    }

    public override string ToString()
    {
        return $"{Type}: {Message} (Line {LineNumber})\n{LineContent}";
        // return $"{Type}: {Message} (Line {LineNumber})\n{LineContent}\nFull: {FullError}";
    }
}

public class LuaScriptValidationException : Exception
{
    public bool IsValid { get; }
    public LuaScriptError? SyntaxError { get; }
    public IReadOnlyList<LuaScriptError> RuntimeErrors { get; }
    public IReadOnlyList<string> Warnings { get; }
    public bool HasErrors => SyntaxError != null || (RuntimeErrors?.Count ?? 0) != 0;

    public LuaScriptValidationException(LuaScriptValidationResult result) : base(BuildMessage(result))
    {
        IsValid = result.IsValid;
        SyntaxError = result.SyntaxError;
        RuntimeErrors = result.RuntimeErrors ?? [];
        Warnings = result.Warnings ?? [];
    }

    private static string BuildMessage(LuaScriptValidationResult result)
    {
        if (result.IsValid) return "Lua script validated successfully.";

        var sb = new StringBuilder();
        var runtimeErrors = result.RuntimeErrors ?? Enumerable.Empty<LuaScriptError>();
        var warnings = result.Warnings ?? Enumerable.Empty<string>();

        if (result.SyntaxError is { })
        {
            AppendError(sb, "[SyntaxError]", result.SyntaxError);
        }

        foreach (var err in runtimeErrors)
        {
            AppendError(sb, "[RuntimeError]", err);
        }

        foreach (var warning in warnings)
        {
            sb.AppendLine($"[Warning] {warning}");
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendError(StringBuilder sb, string prefix, LuaScriptError err)
    {
        sb.Append(prefix);
        if (!string.IsNullOrEmpty(err.Type)) sb.Append($" {err.Type}");
        sb.Append($" - {err.Message}");
        if (err.LineNumber != null) sb.Append($" (Line {err.LineNumber})");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(err.LineContent))
        {
            sb.AppendLine($"> {err.LineContent.Trim()}");
        }
    }
    public override string ToString() => Message;
}