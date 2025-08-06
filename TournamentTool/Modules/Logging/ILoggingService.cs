using TournamentTool.Models;

namespace TournamentTool.Modules.Logging;

public interface ILoggingService
{
    void Log(object message, LogLevel level = LogLevel.Normal);
    void Error(object message);
    void Warning(object message);
    void Information(object message);
    void Debug(object message);
}