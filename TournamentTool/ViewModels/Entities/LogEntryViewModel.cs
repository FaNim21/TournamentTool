using System.Windows;
using System.Windows.Media;
using TournamentTool.Models;

namespace TournamentTool.ViewModels.Entities;

public class LogEntryViewModel : BaseViewModel
{
    private readonly LogEntry _data;

    public string Message => _data.Message;
    public LogLevel Level => _data.Level;
    public string Date => _data.Date.ToString("h:mm:ss tt");
    public string DateMonth => _data.Date.ToString("M");

    private Brush? _borderBrushColor;
    public Brush? BorderBrushColor
    {
        get => _borderBrushColor;
        set
        {
            _borderBrushColor = value;
            OnPropertyChanged(nameof(BorderBrushColor));
        }
    }

    
    public LogEntryViewModel(LogEntry data)
    {
        _data = data;
        Update();
    }

    public void Update()
    {
        Application.Current?.Dispatcher.Invoke(delegate
        {
            BorderBrushColor = _data.Level switch
            {
                LogLevel.Info => new SolidColorBrush(Color.FromRgb(150, 205, 238)),
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 222, 33)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(212, 63, 63)),
                _ => BorderBrushColor
            };
        });
    }
}