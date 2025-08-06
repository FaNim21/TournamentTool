using TournamentTool.Models;

namespace TournamentTool.ViewModels.Entities;

public class LogEntryViewModel : BaseViewModel
{
    private readonly LogEntry _data;

    public string Message => _data.Message;
    public LogLevel Level => _data.Level;
    public string Date => _data.Date.ToString("HH:mm:ss");

    
    public LogEntryViewModel(LogEntry data)
    {
        _data = data;
        
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(Level));
        OnPropertyChanged(nameof(Date));
    }
}