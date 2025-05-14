using System.ComponentModel;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.SidePanels;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services;

public class PaceManService : BaseViewModel
{
    private SidePanel SidePanel { get; set; }
    private TournamentViewModel TournamentViewModel { get; set; }

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

    public PaceManService(SidePanel sidePanel, TournamentViewModel tournamentViewModel)
    {
        SidePanel = sidePanel;
        TournamentViewModel = tournamentViewModel;
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
            PaceManPlayers[i].PlayerViewModel = null;

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

            PlayerViewModel? player = TournamentViewModel.GetPlayerByTwitchName(pace.User.TwitchName!);
            if (TournamentViewModel.IsUsingWhitelistOnPaceMan && player == null) continue;
            if (TournamentViewModel.AddUnknownPlayersToWhitelist && player == null)
            {
                player = AddPaceManPlayerToWhiteList(pace);
            }

            paceViewModel = new PaceManViewModel(this, pace, player!);
            AddPaceMan(paceViewModel);
            isPacemanEmpty = false;
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        OnRefreshGroup?.Invoke(isPacemanEmpty);
    }

    private void AddPaceMan(PaceManViewModel paceman)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Add(paceman); });
        SidePanel.UpdatePlayerStreamData(paceman.Nickname, paceman.TwitchName);
    }
    private void RemovePaceMan(PaceManViewModel paceMan)
    {
        Application.Current?.Dispatcher.Invoke(() => { PaceManPlayers.Remove(paceMan); });
    }

    private PlayerViewModel AddPaceManPlayerToWhiteList(PaceMan paceMan)
    {
        Player player = new Player()
        {
            //TODO: 0 SKONCZYC TO
        };

        PlayerViewModel playerViewModel = new PlayerViewModel();
        
        TournamentViewModel.AddPlayer(playerViewModel);
        return playerViewModel;
    }

    public void EvaluatePlayerInLeaderboard(PaceManViewModel paceman)
    {
        var split =  paceman.GetLastSplit();
        if (split.SplitName.StartsWith("common.")) return;
        
        var milestone = EnumExtensions.FromDescription<RunMilestone>(split.SplitName);
        var data = new LeaderboardPlayerEvaluateData()
        {
            PlayerViewModel = paceman.PlayerViewModel!,
            Milestone = milestone,
            Time = (int)split.IGT
        };
        SidePanel.EvaluatePlayerInLeaderboard(data);
    }

    public bool CheckForGoodPace(SplitType splitType, PacemanPaceMilestone lastMilestone)
    {
        bool isPacePriority = splitType switch
        {
            SplitType.structure_2 => TournamentViewModel.Structure2GoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.first_portal => TournamentViewModel.FirstPortalGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.enter_stronghold => TournamentViewModel.EnterStrongholdGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.enter_end => TournamentViewModel.EnterEndGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.credits => TournamentViewModel.CreditsGoodPaceMiliseconds > lastMilestone.IGT,
            _ => false
        };
        return isPacePriority;
    }
}
