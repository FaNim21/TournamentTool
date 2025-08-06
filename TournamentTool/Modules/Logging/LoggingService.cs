using TournamentTool.Models;

namespace TournamentTool.Modules.Logging;

public class LoggingService : ILoggingService
{
    private readonly LogStore _logStore;
    
    
    public LoggingService(LogStore logStore)
    {
        _logStore = logStore;
    }

    public void Log(object message, LogLevel level = LogLevel.Normal)
    {
        if (message is null) return;

        string logMessage = message.ToString() ?? string.Empty;
        _logStore.AddLog(logMessage, level);
        
        string date = $"[{DateTime.Now:HH:mm:ss}] ";
        string type = level == LogLevel.Normal ? "" : $"[{level}] ";
        ConsoleLogging(date, type + logMessage, level);
    }

    public void Error(object message) => Log(message, LogLevel.Error);
    public void Warning(object message) => Log(message, LogLevel.Warning);
    public void Information(object message) => Log(message, LogLevel.Info);
    public void Debug(object message) => Log(message, LogLevel.Debug);

    private void ConsoleLogging(string date, string message, LogLevel level)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(date);
        
        Console.ForegroundColor = GetLevelColor(level);
        Console.Write(message);
        Console.ResetColor();
        Console.WriteLine();
    }

    private ConsoleColor GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Info => ConsoleColor.Cyan,
        LogLevel.Debug => ConsoleColor.Green,
        _ => ConsoleColor.White,
    };
}