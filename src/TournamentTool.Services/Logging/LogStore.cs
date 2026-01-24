using System.IO.Compression;
using System.Text;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.Logging;

public interface ILogStore
{
    IReadOnlyList<LogEntry> Logs { get; }
    event EventHandler<LogEntry>? LogReceived;
    event EventHandler? LogsCleared;
    
    void AddLog(string message, LogLevel level);
    Task SaveToFileAsync();
}

public class LogStore : ILogStore
{
    private List<LogEntry> _logs { get; } = [];
    public IReadOnlyList<LogEntry> Logs => _logs;

    public event EventHandler<LogEntry>? LogReceived;
    public event EventHandler? LogsCleared;

    
    public void AddLog(string message, LogLevel level)
    {
        var log = new LogEntry(message, level, DateTime.Now);
        _logs.Add(log);
        
        LogReceived?.Invoke(this, log);
    }

    public async Task SaveToFileAsync()
    {
        string fileName = Helper.GetUniqueDateTimeFileName();
        string logsPath = Path.Combine(Consts.LogsPath, fileName + ".log.gz");
        
        await using var fileStream = File.Create(logsPath);
        await using var gzip = new GZipStream(fileStream, CompressionLevel.Optimal);
        await using var writer = new StreamWriter(gzip, Encoding.UTF8);

        foreach (var log in Logs)
        {
            await writer.WriteLineAsync(log.ToString());
        }
        
        ClearLogs();
    }

    private void ClearLogs()
    {
        _logs.Clear();
        LogsCleared?.Invoke(this, EventArgs.Empty);
    }
}