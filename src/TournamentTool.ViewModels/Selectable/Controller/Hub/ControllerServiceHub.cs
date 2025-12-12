using System.Timers;
using TournamentTool.Services;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;

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
    private ILoggingService Logger { get; }

    private readonly Dictionary<string, ServiceRunner> _services = new();
    private System.Timers.Timer _uiUpdateTimer;
    
    
    public ControllerServiceHub(ControllerViewModel controller, ITwitchService twitch, ILoggingService logger, ObsController obs, 
        ITournamentState tournamentState, ITournamentLeaderboardRepository leaderboardRepository, ITournamentPlayerRepository playerRepository)
    {
        Logger = logger;
        
        _uiUpdateTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(100));
        _uiUpdateTimer.Elapsed += UpdateTimers;
        _uiUpdateTimer.AutoReset = true;
        _uiUpdateTimer.Start();

        TwitchUpdaterService twitchUpdater = new(controller, twitch, playerRepository);
        AddService("Twitch-streams", twitchUpdater, TimeSpan.FromSeconds(60), false, false);

        APIUpdaterService apiUpdater = new(controller, logger, obs, tournamentState, leaderboardRepository, playerRepository);
        AddService("API-data", apiUpdater, TimeSpan.FromSeconds(5), true, true);
        //TODO: 0 Tymczasowo zmieniony czas na 5 sekund z racji wypisywania leaderboard api do plikow pod obsa
    }

    public void OnEnable()
    {
        foreach (var runner in _services.Values)
        {
            runner.Service.OnEnable();
            runner.UpdateUI = true;
        }
        
        _uiUpdateTimer.Start();
    }
    public void OnDisable()
    {
        foreach (var runner in _services.Values)
        {
            runner.Service.OnDisable();
            runner.UpdateUI = false;
        }
        
        _uiUpdateTimer.Stop();
    }
    
    public void AddService(string name, IServiceUpdater service, TimeSpan interval, bool runImmediately, bool runFromBeginning)
    {
        if (_services.ContainsKey(name)) return;

        ServiceRunner runner = new(Logger, name, service, interval)
        {
            RunImmediately = runImmediately,
        };
        
        _services[name] = runner;

        if (!runFromBeginning) return;
        runner.Run();
    }
    
    private void UpdateTimers(object? sender, ElapsedEventArgs e)
    {
        foreach (var runner in _services.Values)
        {
            if (!runner.UpdateUI || runner.IsPaused) continue;
            if (runner.Service is not IServiceUpdaterTimer timer) continue;
                
            double elapsedSeconds = runner.Stopwatch.Elapsed.TotalSeconds;
            double totalSeconds = runner.Interval.TotalSeconds;
            
            double remaining = totalSeconds - elapsedSeconds;
            if (remaining < 0)
            {
                timer.UpdateTimer("Updating...");
                continue;
            }
            
            TimeSpan remainingSpan = TimeSpan.FromSeconds(remaining);
            string timeText = remainingSpan.ToString(@"ss\.f");
            timer.UpdateTimer(timeText);
        }
    }

    public void ChangeServiceStatus(string name, bool run)
    {
        if (!_services.TryGetValue(name, out var runner)) return;
        if (runner.IsPaused != run) return;
        
        if (run)
        {
            runner.Run();
        }
        else
        {
            runner.Stop();
        }
    }
}