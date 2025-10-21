using System.ComponentModel;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Background;

public class BackgroundCoordinator : IBackgroundCoordinator, IBackgroundServiceRegistry
{
    private ITournamentPresetManager Tournament { get; }
    private ILoggingService Logger { get; }
    
    private readonly BackgroundServiceFactory _backgroundServiceFactory;
    
    public event EventHandler<ServiceRegistryEventArgs>? ServiceChanged;
    
    public IBackgroundService? Service { get; private set; }
    public List<IBackgroundDataReceiver> Receivers { get; } = [];

    private BackgroundWorker? _worker;
    private CancellationTokenSource? _cancellationTokenSource;

    
    public BackgroundCoordinator(ITournamentPresetManager tournament, ILoggingService logger, IServiceProvider serviceProvider)
    {
        Tournament = tournament;
        Logger = logger;
        Tournament = tournament;
        
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
        if (!isValidated || mode == ControllerMode.None)
        {
            ClearService();
            return;
        }
        
        var service = _backgroundServiceFactory.Create(mode)!;
        Service = service;

        if (_worker == null)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _worker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _worker.DoWork += Update;
            _worker.RunWorkerAsync();
        }

        for (int i = 0; i < Receivers.Count; i++)
        {
            Service.RegisterData(Receivers[i]);
        }
        
        Logger.Log($"New service {service.GetType()} just started");
        ServiceChanged?.Invoke(this, new ServiceRegistryEventArgs(Tournament.ControllerMode, true));
    }

    private async void Update(object? sender, DoWorkEventArgs e)
    {
        try
        {
            var cancellationToken = _cancellationTokenSource!.Token;
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            
            while (!_worker!.CancellationPending && !cancellationToken.IsCancellationRequested)
            {
                if (Service == null) continue;
                await Service.Update(cancellationToken);
            }
        }
        catch (TaskCanceledException) { Clear(); }
        catch (Exception ex)
        {
            Logger.Error($"Error while updating background service: {ex}");
            Clear();
        }
    }

    public void Clear()
    {
        if (_worker != null)
        {
            _worker.CancelAsync();
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            _worker.DoWork -= Update;
            _worker.Dispose();
        }

        _worker = null;
        ClearService();
    }
    private void ClearService()
    {
        if (Service == null) return;
        Logger.Log($"Service {Service!.GetType()} just stopped");
        Service = null;
        
        ServiceChanged?.Invoke(this, new ServiceRegistryEventArgs(Tournament.ControllerMode, false));
    }
}