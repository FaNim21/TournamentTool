namespace TournamentTool.Domain.Entities;

public enum LogLevel
{
    Normal,
    Debug,
    Info,
    Warning,
    Error,
}

public record LogEntry(string Message, LogLevel Level, DateTime Date)
{
    public override string ToString()
    {
        string type = Level == LogLevel.Normal ? "" : $"[{Level}] ";
        
        return $"[{Date:h:mm:ss tt}] {type}{Message}";
    }
}