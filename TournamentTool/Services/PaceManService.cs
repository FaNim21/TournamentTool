using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.RightsManagement;
using System.Text.Json;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services;

public class PaceManService : BaseViewModel
{
    private ControllerViewModel Controller { get; set; }
    private TournamentViewModel TournamentViewModel { get; set; }
    private IPresetSaver PresetSaver { get; set; }

    private BackgroundWorker? _paceManWorker;
    private CancellationTokenSource? _cancellationTokenSource;

    private ObservableCollection<PaceManViewModel> _paceManPlayers = [];
    public ObservableCollection<PaceManViewModel> PaceManPlayers
    {
        get => _paceManPlayers;
        set
        {
            _paceManPlayers = value;
            OnPropertyChanged(nameof(PaceManPlayers));
        }
    }

    public event Action<bool>? OnRefreshGroup;

    public PaceManService(ControllerViewModel controllerViewModel, TournamentViewModel tournamentViewModel, IPresetSaver presetSaver)
    {
        Controller = controllerViewModel;
        TournamentViewModel = tournamentViewModel;
        PresetSaver = presetSaver;
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
                await Task.Delay(TimeSpan.FromMilliseconds(TournamentViewModel.PaceManRefreshRateMiliseconds), cancellationToken);
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
        List<PaceManViewModel> currentPaces = new(PaceManPlayers);

        foreach (var pace in paceMan)
        {
            bool wasPaceFound = false;
            PaceManViewModel? paceViewModel = null;
            
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

            Player? player = TournamentViewModel.GetPlayerByTwitchName(pace.User.TwitchName!);
            if (TournamentViewModel.IsUsingWhitelistOnPaceMan && player == null) continue;

            paceViewModel = new PaceManViewModel(pace, TournamentViewModel, player!);
            AddPaceMan(paceViewModel);
            isPacemanEmpty = false;
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        OnRefreshGroup?.Invoke(isPacemanEmpty);
    }

    public void AddPaceMan(PaceManViewModel paceMan)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Add(paceMan); });

        int n = TournamentViewModel.Players.Count;
        bool updatedPlayer = false;
        for (int i = 0; i < n; i++)
        {
            var player = TournamentViewModel.Players[i];
            if (!player.InGameName!.Equals(paceMan.Nickname, StringComparison.OrdinalIgnoreCase)) continue;
            
            updatedPlayer = true;
            player.StreamData.SetName(paceMan.TwitchName!);
            Controller.FilterItems();
        }

        if (updatedPlayer)
        {
            PresetSaver.SavePreset();
        }
    }
    public void RemovePaceMan(PaceManViewModel paceMan)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Remove(paceMan); });
    }

}
