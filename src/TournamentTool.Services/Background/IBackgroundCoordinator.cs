using TournamentTool.Domain.Enums;

namespace TournamentTool.Services.Background;

public interface IBackgroundCoordinator
{
    void Register(IBackgroundDataReceiver? receiver);
    void Unregister(IBackgroundDataReceiver? receiver);
    void Initialize(ControllerMode mode, bool isValidated);
}

public interface IBackgroundServiceRegistry
{
    event EventHandler<ServiceRegistryEventArgs> ServiceChanged;
}

public class ServiceRegistryEventArgs : EventArgs
{
    public ControllerMode Mode { get; }
    public bool IsWorking { get; }
    
    
    public ServiceRegistryEventArgs(ControllerMode mode, bool isWorking)
    {
        Mode = mode;
        IsWorking = isWorking;
    }
}