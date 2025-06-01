using TournamentTool.Models;
using TournamentTool.Utils;
using System.Text.Json;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services.Background;

public class PaceManService : IBackgroundService
{
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }
    private IPresetSaver PresetSaver { get; }

    private IPacemanDataReceiver? _pacemanSidePanelReceiver;
    private IPlayerAddReceiver? _playerAddReceiver;

    private List<PaceManViewModel> _paceManPlayers = [];
    private List<PaceMan> _paceManData = [];


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
            Console.WriteLine("--------- Startup registering data");
            OrganizingPacemanData();
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
        List<PaceMan>? paceMan = JsonSerializer.Deserialize<List<PaceMan>>(result);
        _paceManData = paceMan ?? [];
        OrganizingPacemanData();
    }

    private void OrganizingPacemanData()
    {
        List<PaceManViewModel> currentPaces = new(_paceManPlayers);

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

            var paceViewModel = new PaceManViewModel(this, pace, player!);
            AddPaceMan(paceViewModel);
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        _pacemanSidePanelReceiver?.ReceivePlayers(_paceManPlayers);
    }

    private void AddPaceMan(PaceManViewModel paceMan)
    {
        _paceManPlayers.Add(paceMan);
        UpdatePlayerStreamData(paceMan.Nickname, paceMan.TwitchName);
    }
    private void RemovePaceMan(PaceManViewModel paceMan)
    {
        _paceManPlayers.Remove(paceMan);
    }
    
    protected void UpdatePlayerStreamData(string inGameName, string twitchName)
    {
        int n = TournamentViewModel.Players.Count;
        bool updatedPlayer = false;
        
        for (int i = 0; i < n; i++)
        {
            var player = TournamentViewModel.Players[i];
            if (!player.InGameName!.Equals(inGameName, StringComparison.OrdinalIgnoreCase)) continue;
            
            updatedPlayer = true;
            player.StreamData.SetName(twitchName);
            if (!TournamentViewModel.IsUsingTwitchAPI)
            {
                player.StreamData.LiveData.Clear(false);
            }
            _pacemanSidePanelReceiver?.FilterItems();
        }
        
        if (updatedPlayer)
        {
            PresetSaver.SavePreset();
        }
    }

    private PlayerViewModel AddPaceManPlayerToWhiteList(PaceMan paceMan)
    {
        Player player = new Player()
        {
            UUID = paceMan.User.UUID!,
            Name = paceMan.Nickname,
            InGameName = paceMan.Nickname,
        };

        PlayerViewModel playerViewModel = new PlayerViewModel(player);
        if (!string.IsNullOrEmpty(paceMan.User.TwitchName))
        {
            playerViewModel.StreamData.SetName(paceMan.User.TwitchName);
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

    public void EvaluatePlayerInLeaderboard(PaceManViewModel paceman)
    {
        if (paceman.PlayerViewModel == null) return;
        
        var split =  paceman.GetLastSplit();
        if (split.SplitName.StartsWith("common.")) return;
        var milestone = EnumExtensions.FromDescription<RunMilestone>(split.SplitName);
        var mainSplit = new LeaderboardTimeline(milestone, (int)split.IGT);
        
        var previousSplit = paceman.GetSplit(2);
        LeaderboardTimeline? pacemanPreviousSplit = null;
        if (previousSplit != null)
        {
            var previousMilestone = EnumExtensions.FromDescription<RunMilestone>(split.SplitName);
            pacemanPreviousSplit = new LeaderboardTimeline(previousMilestone, (int)previousSplit.IGT);
        }
        
        var data = new LeaderboardPacemanEvaluateData(paceman.PlayerViewModel.Data, paceman.WorldID, mainSplit, pacemanPreviousSplit);
        Leaderboard.EvaluatePlayer(data);
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
