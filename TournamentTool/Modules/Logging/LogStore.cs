using TournamentTool.Models;

namespace TournamentTool.Modules.Logging;

public class LogStore
{
    public List<LogEntry> Logs { get; } = [];

    public event EventHandler<LogEntry>? LogReceived;

    public void AddLog(string message, LogLevel level)
    {
        var log = new LogEntry(message, level, DateTime.Now);
        Logs.Add(log);
        
        LogReceived?.Invoke(this, log);
    }   
}