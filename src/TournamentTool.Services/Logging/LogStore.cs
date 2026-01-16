using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Logging;

public class LogStore
{
    private List<LogEntry> _logs { get; } = [];
    public IReadOnlyList<LogEntry> Logs => _logs;

    public event EventHandler<LogEntry>? LogReceived;

    public void AddLog(string message, LogLevel level)
    {
        var log = new LogEntry(message, level, DateTime.Now);
        _logs.Add(log);
        
        LogReceived?.Invoke(this, log);
    }   
}