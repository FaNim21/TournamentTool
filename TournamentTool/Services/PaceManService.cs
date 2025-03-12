using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using TournamentTool.ViewModels;

namespace TournamentTool.Services;

public class PaceManService : BaseViewModel
{
    private ControllerViewModel Controller { get; set; }

    private BackgroundWorker? _paceManWorker;
    private CancellationTokenSource? _cancellationTokenSource;

    private ObservableCollection<PaceMan> _paceManPlayers = [];
    public ObservableCollection<PaceMan> PaceManPlayers
    {
        get => _paceManPlayers;
        set
        {
            _paceManPlayers = value;
            OnPropertyChanged(nameof(PaceManPlayers));
        }
    }

    public event Action<bool>? OnRefreshGroup;

    public PaceManService(ControllerViewModel controllerViewModel)
    {
        Controller = controllerViewModel;
    }

    public override void OnEnable(object? parameter)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _paceManWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
        _paceManWorker.DoWork += PaceManUpdate;
        _paceManWorker.RunWorkerAsync();
    }
    public override bool OnDisable()
    {
        _paceManWorker?.CancelAsync();
        _cancellationTokenSource?.Cancel();
        _paceManWorker?.Dispose();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _paceManWorker = null;

        for (int i = 0; i < PaceManPlayers.Count; i++)
            PaceManPlayers[i].Player = null;

        PaceManPlayers.Clear();

        return true;
    }

    private async void PaceManUpdate(object? sender, DoWorkEventArgs e)
    {
        var cancellationToken = _cancellationTokenSource!.Token;

        while (!_paceManWorker!.CancellationPending && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RefreshPaceManAsync();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Controller.TournamentViewModel.PaceManRefreshRateMiliseconds), cancellationToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }
    private async Task RefreshPaceManAsync()
    {
        string result = await Helper.MakeRequestAsString(Consts.PaceManAPI);
        List<PaceMan>? paceMan = JsonSerializer.Deserialize<List<PaceMan>>(result);
        if (paceMan == null) return;

        bool isPacemanEmpty = true;
        List<PaceMan> currentPaces = new(PaceManPlayers);

        for (int i = 0; i < paceMan.Count; i++)
        {
            var pace = paceMan[i];
            bool wasPaceFound = false;

            if (!pace.IsLive() || pace.IsHidden || pace.IsCheated) continue;

            for (int j = 0; j < currentPaces.Count; j++)
            {
                var currentPace = currentPaces[j];
                if (!pace.Nickname.Equals(currentPace.Nickname, StringComparison.OrdinalIgnoreCase)) continue;
                
                wasPaceFound = true;
                isPacemanEmpty = false;
                currentPace.Update(pace);
                currentPaces.Remove(currentPace);
                break;
            }

            if (wasPaceFound) continue;

            int n = Controller.TournamentViewModel.Players.Count;
            for (int j = 0; j < n; j++)
            {
                var current = Controller.TournamentViewModel.Players[j];
                if (!current.StreamData.ExistName(pace.User.TwitchName!)) continue;
                
                pace.Player = current;
                break;
            }

            if (Controller.TournamentViewModel.IsUsingWhitelistOnPaceMan && pace.Player == null) continue;

            pace.Initialize(Controller, pace.Splits);
            AddPaceMan(pace);
            isPacemanEmpty = false;
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        OnRefreshGroup?.Invoke(isPacemanEmpty);
    }

    public void AddPaceMan(PaceMan paceMan)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Add(paceMan); });

        int n = Controller.TournamentViewModel.Players.Count;
        bool updatedPlayer = false;
        for (int i = 0; i < n; i++)
        {
            var player = Controller.TournamentViewModel.Players[i];

            if (player.InGameName!.Equals(paceMan.Nickname, StringComparison.OrdinalIgnoreCase))
            {
                updatedPlayer = true;
                player.StreamData.SetName(paceMan.User.TwitchName!);
                Controller.FilterItems();
            }
        }

        if (updatedPlayer)
        {
            Controller.SavePreset();
        }
    }
    public void RemovePaceMan(PaceMan paceMan)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Remove(paceMan); });
    }

}
