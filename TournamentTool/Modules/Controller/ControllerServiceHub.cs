using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.Controller;

public interface IServiceUpdaterTimer
{
    public void UpdateTimer(string time);
}
public interface IServiceUpdater
{
    Task UpdateAsync(CancellationToken token);
    void OnEnable();
    void OnDisable();
}

public class ControllerServiceHub
{
    private class ServiceRunner
    {
        public string Name { get; }
        public IServiceUpdater Service { get; }
        public TimeSpan Interval { get; }
        public Task? RunningTask { get; set; }
        public bool IsEnabled { get; set; } = true;

        public DateTime LastUpdate { get; set; }

        
        public ServiceRunner(string name, IServiceUpdater service, TimeSpan interval)
        {
            Name = name;
            Service = service;
            Interval = interval;
        }
    }
    
    private ILoggingService Logger { get; }

    private readonly Dictionary<string, ServiceRunner> _services = new();
    private readonly CancellationTokenSource _cancellationSource = new();
    
    
    public ControllerServiceHub(ControllerViewModel controller, TwitchService twitch, ILoggingService logger, TournamentViewModel preset, ObsController obs)
    {
        Logger = logger;
        var _uiUpdateTimer = new Timer(UpdateTimers, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        
        TwitchUpdaterService twitchUpdater = new(controller, twitch);
        AddService("Twitch-streams", twitchUpdater, TimeSpan.FromSeconds(60));

        APIUpdaterService apiUpdater = new(controller, logger, preset, obs);
        AddService("API-data", apiUpdater, TimeSpan.FromSeconds(5));
        //TODO: 0 Tymczasowo zmieniony czas na 5 sekund z racji wypisywania leaderboard api do plikow pod obsa
    }
    public void OnEnable()
    {
        foreach (var runner in _services.Values)
        {
            runner.Service.OnEnable();
            runner.IsEnabled = true;
        }
    }
    public void OnDisable()
    {
        foreach (var runner in _services.Values)
        {
            runner.Service.OnDisable();
            runner.IsEnabled = false;
        }
    }
    
    public void AddService(string name, IServiceUpdater service, TimeSpan interval)
    {
        if (_services.ContainsKey(name)) return;
            
        var runner = new ServiceRunner(name, service, interval);
        _services[name] = runner;
        
        runner.RunningTask = RunServiceAsync(runner);
    }
    
    private async Task RunServiceAsync(ServiceRunner runner)
    {
        try
        {
            while (!_cancellationSource.Token.IsCancellationRequested)
            {
                if (runner.IsEnabled)
                {
                    try
                    {
                        runner.LastUpdate = DateTime.Now;
                        await runner.Service.UpdateAsync(_cancellationSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error in service '{runner.Name}': {ex.Message}");
                    }
                }
                
                await Task.Delay(runner.Interval, _cancellationSource.Token);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
        }
    }
    
    private void UpdateTimers(object? state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            foreach (var runner in _services.Values)
            {
                if (runner.Service is not IServiceUpdaterTimer timer) continue;
                if (runner.LastUpdate == DateTime.MinValue) continue;
                
                var elapsed = DateTime.Now - runner.LastUpdate;
                var remaining = runner.Interval - elapsed;
                    
                var time = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                string TimeToNextUpdateText = time.TotalSeconds > 0 ? $"{time:mm\\:ss}" : "Updating...";
                timer.UpdateTimer(TimeToNextUpdateText);
            }
        });
    }
}