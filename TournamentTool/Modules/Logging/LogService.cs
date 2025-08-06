using TournamentTool.Models;

namespace TournamentTool.Modules.Logging;

public static class LogService
{
    public static ILoggingService? Logger { get; private set; }

    public static void Initialize(ILoggingService logger) => Logger = logger;

    public static void Log(object message, LogLevel level = LogLevel.Normal) => Logger?.Log(message, level);
    public static void Error(object message) => Logger?.Error(message);
    public static void Warning(object message) => Logger?.Warning(message);
    public static void Information(object message) => Logger?.Information(message);
    public static void Debug(object message) => Logger?.Debug(message);
}