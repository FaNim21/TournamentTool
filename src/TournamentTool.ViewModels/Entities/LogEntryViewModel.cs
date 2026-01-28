using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;

namespace TournamentTool.ViewModels.Entities;

public class LogEntryViewModel : BaseViewModel
{
    private readonly LogEntry _data;
    private readonly IDispatcherService _dispatcher;

    public string Message => _data.Message;
    public LogLevel Level => _data.Level;
    public string Date => $"[{_data.Date:h:mm:ss}]";
    public string DateMonth => $"[{_data.Date:M}]";

    public string Type { get; private set; } = string.Empty;

    private string _logLevelColor = "#FFFFFF";
    public string LogLevelColor
    {
        get => _logLevelColor;
        set
        {
            _logLevelColor = value;
            OnPropertyChanged(nameof(LogLevelColor));
        }
    }

    
    public LogEntryViewModel(LogEntry data, IDispatcherService dispatcher) : base(dispatcher)
    {
        _data = data;
        _dispatcher = dispatcher;
        Update();
    }

    public void Update()
    {
        _dispatcher.Invoke(delegate 
        {
            Type = Level == LogLevel.Normal ? "" : $"[{Level}] ";
            
            LogLevelColor = _data.Level switch
            {
                LogLevel.Info => Consts.InfoColor,
                LogLevel.Warning => Consts.WarningColor,
                LogLevel.Debug => Consts.DebugColor,
                LogLevel.Error => Consts.ErrorColor,
                _ => LogLevelColor
            };
        });
    }
}