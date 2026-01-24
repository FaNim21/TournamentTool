using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Logging;

public class LoggingService : ILoggingService
{
    private readonly ILogStore _logStore;
    
    
    public LoggingService(ILogStore logStore)
    {
        _logStore = logStore;
    }

    public void Log(object message, LogLevel level = LogLevel.Normal)
    {
        if (message is null) return;

        string logMessage = message.ToString() ?? string.Empty;
        _logStore.AddLog(logMessage, level);
    }

    public void Error(object message) => Log(message, LogLevel.Error);
    public void Warning(object message) => Log(message, LogLevel.Warning);
    public void Information(object message) => Log(message, LogLevel.Info);
    public void Debug(object message) => Log(message, LogLevel.Debug);
}