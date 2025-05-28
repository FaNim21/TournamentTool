using System.ComponentModel;
using System.IO;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;

namespace TournamentTool.Services;

public class BackgroundCoordinator : IBackgroundCoordinator
{
    public IBackgroundService? Service { get; private set; }
    public List<IBackgroundDataReceiver> Receivers { get; private set; } = [];

    private BackgroundWorker? _worker;
    private CancellationTokenSource? _cancellationTokenSource;
    
    
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

    public void Initialize(IBackgroundService backgroundService)
    {
        Service = backgroundService;

        _cancellationTokenSource = new CancellationTokenSource();
        _worker = new BackgroundWorker { WorkerSupportsCancellation = true };
        _worker.DoWork += Update;
        _worker.RunWorkerAsync();

        for (int i = 0; i < Receivers.Count; i++)
        {
            Service.RegisterData(Receivers[i]);
        }
        
        Console.WriteLine($"Service {backgroundService.GetType()} just started");
    }

    private async void Update(object? sender, DoWorkEventArgs e)
    {
        var cancellationToken = _cancellationTokenSource!.Token;

        while (!_worker!.CancellationPending && !cancellationToken.IsCancellationRequested)
        {
            if (Service == null) break;

            try
            {
                await Service.Update(cancellationToken);
            }
            catch (TaskCanceledException) { Clear(); }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} while updating background service", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                Clear();
            }
        }
    }

    public void Clear()
    {
        if (_worker != null)
        {
            try
            {
                _worker.CancelAsync();
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _worker.DoWork -= Update;
            _worker.Dispose();
        }
        _cancellationTokenSource?.Dispose();
        
        if (Service == null) return;
        Console.WriteLine($"Service {Service!.GetType()} just stopped");
        Service = null;
    }
}