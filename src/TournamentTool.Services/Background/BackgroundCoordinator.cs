using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Logging.Profiling;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Background;

[Profile]
public class BackgroundCoordinator : IBackgroundCoordinator, IBackgroundServiceRegistry
{
    private readonly ITournamentState _tournamentState;
    private ILoggingService Logger { get; }
    
    private readonly BackgroundServiceFactory _backgroundServiceFactory;
    
    public event EventHandler<ServiceRegistryEventArgs>? ServiceChanged;
    
    public IBackgroundService? Service { get; private set; }
    public List<IBackgroundDataReceiver> Receivers { get; } = [];

    private Task? _taskWorker;
    private CancellationTokenSource? _cancellationTokenSource;

    
    public BackgroundCoordinator(ITournamentState tournamentState, ILoggingService logger, IServiceProvider serviceProvider)
    {
        _tournamentState = tournamentState;
        Logger = logger;
        
        _backgroundServiceFactory = new BackgroundServiceFactory(serviceProvider);
    }
    
    public void Register(IBackgroundDataReceiver? receiver)
    {
        if (receiver == null || Receivers.Contains(receiver)) return;
        
        Receivers.Add(receiver);
        Service?.RegisterData(receiver);
    }
    public void Unregister(IBackgroundDataReceiver? receiver)
    {
        if (receiver == null) return;
        
        Receivers.Remove(receiver);
        Service?.UnregisterData(receiver);
    }

    public void Initialize(ControllerMode mode, bool isValidated)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        catch { /**/ }

        if (!isValidated || mode == ControllerMode.None)
        {
            ClearService();
            return;
        }
        
        var service = _backgroundServiceFactory.Create(mode)!;
        Service = service;

        _cancellationTokenSource = new CancellationTokenSource();
        _taskWorker = Task.Run(async () => await UpdateAsync(service, _cancellationTokenSource.Token), _cancellationTokenSource.Token);

        for (int i = 0; i < Receivers.Count; i++)
        {
            Service.RegisterData(Receivers[i]);
        }
        
        Logger.Debug($"New service {service.GetType()} just started");
        ServiceChanged?.Invoke(this, new ServiceRegistryEventArgs(_tournamentState.CurrentPreset.ControllerMode, true));
    }

    private async Task UpdateAsync(IBackgroundService activeService, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), token);

            while (!token.IsCancellationRequested && Service == activeService)
            {
                if (Service == null) break;
                await Service.Update(token);

                if (activeService.DelayMiliseconds <= 1000) break;
                await Task.Delay(TimeSpan.FromMilliseconds(activeService.DelayMiliseconds), token);
            }
        }
        catch (OperationCanceledException)
        {
            string serviceName = Service != null ? Service.GetType().ToString() : _tournamentState.CurrentPreset.ControllerMode.ToString();
            Logger.Debug($"Background service '{serviceName}' was canceled");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error while updating background service: {ex}");
        }
        finally
        {
            if (Service == activeService)
            {
                ClearService();
            }
        }
    }

    public void Clear()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _taskWorker = null;
        ClearService();
    }
    
    private void ClearService()
    {
        if (Service == null) return;
        Logger.Debug($"Service {Service!.GetType()} just stopped");
        Service = null;
        
        ServiceChanged?.Invoke(this, new ServiceRegistryEventArgs(_tournamentState.CurrentPreset.ControllerMode, false));
    }
}