using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Services;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.Controller;

public interface ServiceUpdater
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
        public ServiceUpdater Service { get; }
        public TimeSpan Interval { get; }
        public Task? RunningTask { get; set; }
        public bool IsEnabled { get; set; } = true;

        public DateTime LastUpdate { get; set; }

        
        public ServiceRunner(string name, ServiceUpdater service, TimeSpan interval)
        {
            Name = name;
            Service = service;
            Interval = interval;
        }
    }

    private readonly ControllerViewModel _controller;
    
    private readonly Dictionary<string, ServiceRunner> _services = new();
    private readonly CancellationTokenSource _cancellationSource = new();
    
    private Timer? _uiUpdateTimer;

    public ControllerServiceHub(ControllerViewModel controller, TwitchService twitch)
    {
        _controller = controller;
        
        _uiUpdateTimer = new Timer(UpdateTimers, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        
        TwitchUpdaterService twitchUpdater = new(controller, twitch);
        AddService("Twitch-streams", twitchUpdater, TimeSpan.FromSeconds(60));

        APIUpdaterService apiUpdater = new(controller);
        AddService("API-data", apiUpdater, TimeSpan.FromSeconds(1));
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
    
    public void AddService(string name, ServiceUpdater service, TimeSpan interval)
    {
        if (_services.ContainsKey(name)) return;
            
        var runner = new ServiceRunner(name, service, interval);
        _services[name] = runner;
        
        runner.RunningTask = RunServiceAsync(runner);
        return;
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
                        Console.WriteLine($"Error in service '{runner.Name}': {ex.Message}");
                    }
                }
                
                await Task.Delay(runner.Interval, _cancellationSource.Token);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void UpdateTimers(object? state)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var runner = _services["Twitch-streams"];
            if (runner.LastUpdate == DateTime.MinValue) return;
                
            var elapsed = DateTime.Now - runner.LastUpdate;
            var remaining = runner.Interval - elapsed;
                    
            var time = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            string TimeToNextUpdateText = time.TotalSeconds > 0 ? $"{time:mm\\:ss}" : "Updating...";
            _controller.TwitchUpdateProgressText = TimeToNextUpdateText;
        });
    }
}