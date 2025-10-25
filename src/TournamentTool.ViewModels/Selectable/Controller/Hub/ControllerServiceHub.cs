using System.Timers;
using TournamentTool.Services;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable.Controller.Hub;

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

    private System.Timers.Timer _uiUpdateTimer;
    
    
    public ControllerServiceHub(ControllerViewModel controller, ITwitchService twitch, ILoggingService logger, ObsController obs, 
        ITournamentState tournamentState, ITournamentLeaderboardRepository leaderboardRepository, ITournamentPlayerRepository playerRepository)
    {
        Logger = logger;
        
        _uiUpdateTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(250));
        _uiUpdateTimer.Elapsed += UpdateTimers;
        _uiUpdateTimer.AutoReset = true;
        _uiUpdateTimer.Start();

        TwitchUpdaterService twitchUpdater = new(controller, twitch, playerRepository);
        AddService("Twitch-streams", twitchUpdater, TimeSpan.FromSeconds(60));

        APIUpdaterService apiUpdater = new(controller, logger, obs, tournamentState, leaderboardRepository, playerRepository);
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
        
        _uiUpdateTimer.Start();
    }
    public void OnDisable()
    {
        foreach (var runner in _services.Values)
        {
            runner.Service.OnDisable();
            runner.IsEnabled = false;
        }
        
        _uiUpdateTimer.Stop();
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
    
    private void UpdateTimers(object? sender, ElapsedEventArgs e)
    {
        //TODO: 0 czy ten dispatcher fr jest potrzebny tu? xd
        /*
        Application.Current?.Dispatcher.Invoke(() =>
        {
            */
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
        // });
    }
}