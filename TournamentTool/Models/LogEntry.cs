namespace TournamentTool.Models;

public enum LogLevel
{
    Normal,
    Debug,
    Info,
    Warning,
    Error,
}

public record LogEntry(string Message, LogLevel Level, DateTime Date);