using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;

namespace TournamentTool.Services;

public class BackgroundCoordinator : IBackgroundCoordinator
{
    public IBackgroundService? Service { get; private set; }
    public IBackgroundDataReceiver? Receiver { get; private set; }

    private BackgroundWorker? _worker;
    private CancellationTokenSource? _cancellationTokenSource;
    
    
    public void Register(IBackgroundDataReceiver? receiver)
    {
        if (receiver == null) return;
        
        Receiver = receiver;
        Service?.RegisterData(Receiver);
        string output = Receiver == null ? "\"null\"" : Receiver.GetType().ToString();
        Console.WriteLine($"Receiver {output} has been connected");
    }
    public void Unregister(IBackgroundDataReceiver? receiver)
    {
        if (receiver == null) return;
        
        Service?.UnregisterData(receiver);
        Console.WriteLine($"Receiver {receiver.GetType()} has been disconnected");
        Receiver = null;
    }

    public void Initialize(IBackgroundService backgroundService)
    {
        if (_worker != null)
        {
            _worker.CancelAsync();
            _cancellationTokenSource?.Cancel();
            _worker.DoWork -= Update;
            _worker.Dispose();
            _cancellationTokenSource?.Dispose();
            
            Console.WriteLine($"Service {Service!.GetType()} just stopped");
        }

        Service = backgroundService;

        _cancellationTokenSource = new CancellationTokenSource();
        _worker = new BackgroundWorker { WorkerSupportsCancellation = true };
        _worker.DoWork += Update;
        _worker.RunWorkerAsync();

        if (Receiver != null)
        {
            Service.RegisterData(Receiver);
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
            catch (TaskCanceledException) { break; }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    public void Clear()
    {
        if (_worker != null)
        {
            _worker.CancelAsync();
            _cancellationTokenSource?.Cancel();
            _worker.DoWork -= Update;
            _worker.Dispose();
        }
        _cancellationTokenSource?.Dispose();
        Service = null;
    }
}