using TournamentTool.Core.Extensions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.External;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Background;

public class PaceManService : IBackgroundService
{
    private readonly IPlayerViewModelFactory _playerViewModelFactory;
    private readonly IPacemanAPIService _pacemanApiService;
    private readonly IImageService _imageService;
    private ITournamentPresetManager Tournament { get; }
    private ILeaderboardManager Leaderboard { get; }
    private IPresetSaver PresetSaver { get; }
    public ISettings SettingsService { get; }
    private IDispatcherService Dispatcher { get; }

    private IPacemanDataReceiver? _pacemanSidePanelReceiver;
    private IPlayerAddReceiver? _playerAddReceiver;

    private List<Paceman> _paces = [];
    
    private bool _blockFirstPacemanRefresh = true;


    public PaceManService(ITournamentPresetManager tournament, ILeaderboardManager leaderboard, IPresetSaver presetSaver, IPlayerViewModelFactory playerViewModelFactory, ISettings settingsService, IPacemanAPIService pacemanApiService, IImageService imageService, IDispatcherService dispatcher)
    {
        Tournament = tournament;
        Tournament = tournament;
        _playerViewModelFactory = playerViewModelFactory;
        _pacemanApiService = pacemanApiService;
        _imageService = imageService;
        Leaderboard = leaderboard;
        PresetSaver = presetSaver;
        SettingsService = settingsService;
        Dispatcher = dispatcher;
    }

    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver is IPacemanDataReceiver pacemanDataReceiver)
        {
            _pacemanSidePanelReceiver = pacemanDataReceiver;
            _pacemanSidePanelReceiver.AddPaces(_paces);
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
        await OrganizingPacemanData();
        await Task.Delay(TimeSpan.FromMilliseconds(Tournament.PaceManRefreshRateMiliseconds), token);
    }
    private async Task OrganizingPacemanData()
    {
        var paceManData = await _pacemanApiService.GetPacemanLiveData(); 
        List<Paceman> currentPaces = new(_paces);

        foreach (var pace in paceManData)
        {
            bool wasPaceFound = false;

            if (pace.IsHidden || pace.IsCheated) continue;
            if (!pace.IsLive() && Tournament.ShowOnlyLive) continue;
            pace.ShowOnlyLive = Tournament.ShowOnlyLive;
            
            for (int j = 0; j < currentPaces.Count; j++)
            {
                var currentPace = currentPaces[j];
                if (!pace.Nickname.Equals(currentPace.Nickname, StringComparison.OrdinalIgnoreCase)) continue;
                if (Tournament.IsUsingWhitelistOnPaceMan && currentPace.Player == null) break;
                
                wasPaceFound = true;
                currentPace.Update(pace);
                currentPaces.Remove(currentPace);
                break;
            }

            if (wasPaceFound) continue;

            IPlayerViewModel? player = Tournament.GetPlayerByIGN(pace.Nickname);
            if (Tournament.IsUsingWhitelistOnPaceMan && player == null) continue;
            if (Tournament.AddUnknownPacemanPlayersToWhitelist && player == null)
            {
                player = AddPaceManPlayerToWhiteList(pace);
            }

            var paceman = new Paceman(this, pace, player.Data!);
            UpdateHeadImage(paceman);
            
            AddPaceMan(paceman);
        }

        for (int i = 0; i < currentPaces.Count; i++)
            RemovePaceMan(currentPaces[i]);

        _pacemanSidePanelReceiver?.Update();
        _blockFirstPacemanRefresh = false;
    }
    
    private void UpdateHeadImage(Paceman paceman)
    {
        if (paceman.HeadImage != null) return;

        if (paceman.Player == null)
        {
            string url = SettingsService.Settings.HeadAPIType.GetHeadURL(paceman.UUID, 8);
            paceman.HeadImageOpacity = 0.35f;
            Task.Run(async () =>
            {
                paceman.HeadImage = await _imageService.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            paceman.HeadImageOpacity = 1f;
            byte[] stream = paceman.Player.ImageStream ?? [];
            paceman.HeadImage = _imageService.LoadImageFromStream(stream);
        }
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

        var player = Tournament.GetPlayerByIGN(inGameName);
        if (player == null) return;
        
        player.StreamData.SetName(twitchName);
        if (!Tournament.IsUsingTwitchAPI)
        {
            player.StreamData.LiveData.Clear(false);
        }
    }

    private IPlayerViewModel AddPaceManPlayerToWhiteList(PaceManData paceManData)
    {
        Player player = new Player()
        {
            UUID = paceManData.User.UUID!.Replace("-", ""),
            Name = paceManData.Nickname,
            InGameName = paceManData.Nickname,
        };

        IPlayerViewModel playerViewModel = _playerViewModelFactory.Create(player);
        if (!string.IsNullOrEmpty(paceManData.User.TwitchName))
        {
            playerViewModel.SetStreamName(paceManData.User.TwitchName);
        }
        
        playerViewModel.UpdateHeadImage();

        if (_playerAddReceiver != null)
        {
            _playerAddReceiver.Add(playerViewModel);
        }
        else
        {
            Tournament.AddPlayer(playerViewModel);
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
            SplitType.structure_2 => Tournament.Structure2GoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.first_portal => Tournament.FirstPortalGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.enter_stronghold => Tournament.EnterStrongholdGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.enter_end => Tournament.EnterEndGoodPaceMiliseconds > lastMilestone.IGT,
            SplitType.credits => Tournament.CreditsGoodPaceMiliseconds > lastMilestone.IGT,
            _ => false
        };
        return isPacePriority;
    }
}
