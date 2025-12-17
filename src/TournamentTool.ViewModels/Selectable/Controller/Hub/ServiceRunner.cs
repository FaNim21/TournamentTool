using System.Diagnostics;
using TournamentTool.Services.Logging;

namespace TournamentTool.ViewModels.Selectable.Controller.Hub;

public class ServiceRunner
{
    private ILoggingService Logger { get; }
    public string Name { get; }
    public IServiceUpdater Service { get; }
    public Stopwatch Stopwatch { get; } = new(); //Mozna po i tak zmianie logiki servicer runnera przywrocic wydajniejsza forme mierzenia czyli datetime
    public TimeSpan Interval { get; }
    public Task? RunningTask { get; set; }
    public CancellationTokenSource CancellationSource { get; set; }  = new();

    public bool RunImmediately { get; init; }
    public bool UpdateUI { get; set; } = true;
    public bool IsPaused { get; private set; } = true;


    public ServiceRunner(ILoggingService logger, string name, IServiceUpdater service, TimeSpan interval)
    {
        Logger = logger;
        Name = name;
        Service = service;
        Interval = interval;
    }

    public void Run()
    {
        ResetTimer();
        StartTimer();
            
        CancellationSource = new CancellationTokenSource();
        RunningTask = RunServiceAsync();
    }
    public void Stop()
    {
        CancelAndDispose();
        PauseTimer();
    }
        
    private async Task RunServiceAsync()
    {
        var token = CancellationSource.Token;
        
        try
        {
            ResetTimer();
            StartTimer();

            if (RunImmediately)
            {
                await Service.UpdateAsync(token);
            }
            
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(Interval, token);
                
                await Service.UpdateAsync(token);
                
                ResetTimer();
                StartTimer();
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.Error($"Error in service '{Name}': {ex.Message}");
        }
    }
        
    public void StartTimer()
    {
        if (!IsPaused) return;
            
        Stopwatch.Start();
        IsPaused = false;
    }
    public void PauseTimer()
    {
        if (IsPaused) return;
            
        Stopwatch.Stop();
        IsPaused = true;
    }
    public void ResetTimer()
    {
        PauseTimer();
        Stopwatch.Reset();
    }
        
    public void CancelAndDispose()
    {
        try
        {
            CancellationSource.Cancel();
        }
        catch { /**/ }

        CancellationSource.Dispose();
    }
}