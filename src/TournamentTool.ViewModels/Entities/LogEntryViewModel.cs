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
    public string Date => _data.Date.ToString("h:mm:ss tt");
    public string DateMonth => _data.Date.ToString("M");

    private string _borderBrushColor = string.Empty;
    public string BorderBrushColor
    {
        get => _borderBrushColor;
        set
        {
            _borderBrushColor = value;
            OnPropertyChanged(nameof(BorderBrushColor));
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
            BorderBrushColor = _data.Level switch
            {
                LogLevel.Info => Consts.InfoColor,
                LogLevel.Warning => Consts.WarningColor,
                LogLevel.Error => Consts.ErrorColor,
                _ => BorderBrushColor
            };
        });
    }
}