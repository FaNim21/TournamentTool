namespace TournamentTool.Services.State;

public interface IApplicationState
{
    bool IsWindowBlocked { get; set; }

    event EventHandler<bool> WindowBlockedChanged;
}

public class ApplicationState : IApplicationState
{
    private bool _isWindowBlocked;
    public bool IsWindowBlocked
    {
        get => _isWindowBlocked;
        set
        {
            _isWindowBlocked = value;
            WindowBlockedChanged?.Invoke(this, value);
        }
    }
    
    public event EventHandler<bool>? WindowBlockedChanged;
}