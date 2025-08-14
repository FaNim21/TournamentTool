using TournamentTool.Models;
using TournamentTool.Utils;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MethodTimer;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services.Background;

public class PaceManService : IBackgroundService
{
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }
    private IPresetSaver PresetSaver { get; }

    private IPacemanDataReceiver? _pacemanSidePanelReceiver;
    private IPlayerAddReceiver? _playerAddReceiver;

    private List<Paceman> _paces = [];
    private List<PaceManData> _paceManData = [];
    
    private const int UiSendBatchSize = 2;
    private bool _blockFirstPacemanRefresh = true;


    public PaceManService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard, IPresetSaver presetSaver)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        PresetSaver = presetSaver;
    }

    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver is IPacemanDataReceiver pacemanDataReceiver)
        {
            _pacemanSidePanelReceiver = pacemanDataReceiver;
            
            foreach (var batch in _paces.Batch(UiSendBatchSize))
            {
                Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    foreach (var player in batch)
                    {
                        _pacemanSidePanelReceiver?.AddPace(player);
                    }
                }, DispatcherPriority.Background);
            }
        }
        else if (receiver is IPlayerAddReceiver addPlayerReceiver)
        {
            _playerAddReceiver = addPlayerReceiver;
        }
    }
    public void UnregisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver == _pacemanSidePanelReceiver) _pacemanSidePanelReceiver = null;
        if (receiver == _playerAddReceiver) _playerAddReceiver = null;
    }

    public async Task Update(CancellationToken token)
    {
        await RefreshPaceManAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(TournamentViewModel.PaceManRefreshRateMiliseconds), token);
    }
    private async Task RefreshPaceManAsync()
    {
        string result = await Helper.MakeRequestAsString(Consts.PaceManAPI);
        List<PaceManData>? paceMan = JsonSerializer.Deserialize<List<PaceManData>>(result);
        _paceManData = paceMan ?? [];
        OrganizingPacemanData();
    }

    private void OrganizingPacemanData()
    {
        List<Paceman> currentPaces = new(_paces);

        foreach (var pace in _paceManData)
        {
            bool wasPaceFound = false;

            if (pace.IsHidden || pace.IsCheated) continue;
            if (!pace.IsLive() && TournamentViewModel.ShowOnlyLive) continue;
            pace.ShowOnlyLive = TournamentViewModel.ShowOnlyLive;
            
            for (int j = 0; j < currentPaces.Count; j++)
            {
                var currentPace = currentPaces[j];
                if (!pace.Nickname.Equals(currentPace.Nickname, StringComparison.OrdinalIgnoreCase)) continue;
                if (TournamentViewModel.IsUsingWhitelistOnPaceMan && currentPace.PlayerViewModel == null) break;
                
                wasPaceFound = true;
                currentPace.Update(pace);
                currentPaces.Remove(currentPace);
                break;
            }

            if (wasPaceFound) continue;

            PlayerViewModel? player = TournamentViewModel.GetPlayerByIGN(pace.Nickname);
            if (TournamentViewModel.IsUsingWhitelistOnPaceMan && player == null) continue;
            if (TournamentViewModel.AddUnknownPacemanPlayersToWhitelist && player == null)
            {
                player = AddPaceManPlayerToWhiteList(pace);
            }

            var paceman = new Paceman(this, pace, player!);
            AddPaceMan(paceman);
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        _pacemanSidePanelReceiver?.Update();
        _blockFirstPacemanRefresh = false;
    }
    
    private void AddPaceMan(Paceman paceman)
    {
        _paces.Add(paceman);
        UpdatePlayerStreamData(paceman.Nickname, paceman.StreamDisplayInfo.Name);
        _pacemanSidePanelReceiver?.AddPace(paceman);
    }
    private void RemovePaceMan(Paceman paceman)
    {
        _paces.Remove(paceman);
        _pacemanSidePanelReceiver?.Remove(paceman);
    }
    
    protected void UpdatePlayerStreamData(string inGameName, string? twitchName)
    {
        if (string.IsNullOrWhiteSpace(twitchName)) return;

        var player = TournamentViewModel.GetPlayerByIGN(inGameName);
        if (player == null) return;
        
        player.StreamData.SetName(twitchName);
        if (!TournamentViewModel.IsUsingTwitchAPI)
        {
            player.StreamData.LiveData.Clear(false);
        }
        PresetSaver.SavePreset();
    }

    private PlayerViewModel AddPaceManPlayerToWhiteList(PaceManData paceManData)
    {
        Player player = new Player()
        {
            UUID = paceManData.User.UUID!.Replace("-", ""),
            Name = paceManData.Nickname,
            InGameName = paceManData.Nickname,
        };

        PlayerViewModel playerViewModel = new PlayerViewModel(player);
        if (!string.IsNullOrEmpty(paceManData.User.TwitchName))
        {
            playerViewModel.StreamData.SetName(paceManData.User.TwitchName);
        }
        playerViewModel.UpdateHeadImage();

        if (_playerAddReceiver != null)
        {
            _playerAddReceiver.Add(playerViewModel);
        }
        else
        {
            TournamentViewModel.AddPlayer(playerViewModel);
        }
        return playerViewModel;
    }

    public void AddEvaluationData(Player player, string worldId, LeaderboardTimeline main, LeaderboardTimeline? previous = null)
    {
        if (_blockFirstPacemanRefresh) return;
        
        var data = new LeaderboardPacemanEvaluateData(player, worldId, main, previous);
        Leaderboard.EvaluateData(data);
    }

    public bool CheckForGoodPace(SplitType splitType, PacemanTimeline lastMilestone)
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
