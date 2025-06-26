using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services.Background;

public class RankedService : IBackgroundService
{
    private readonly RankedManagementData? _rankedManagementData;
    
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }

    private IRankedDataReceiver? _rankedDataReceiver;
    private IRankedManagementDataReceiver? _rankedManagementDataReceiver;
    private IPlayerAddReceiver? _playerManagerReceiver;
    
    private long _startTime;
    private int _completedRunsCount;
    
    private List<RankedPace> _paces = [];
    private Dictionary<RankedSplitType, PrivRoomBestSplit> _bestSplits = [];
    
    private readonly JsonSerializerOptions _options;
    private MatchStatus _lastStatus;
    
    
    public RankedService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
        _rankedManagementData = TournamentViewModel.ManagementData as RankedManagementData;
        
        _options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            PropertyNameCaseInsensitive = true
        };
    }
    
    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver is IRankedDataReceiver ranked)
        {
            _rankedDataReceiver = ranked;
            for (int i = 0; i < _paces.Count; i++)
            {
                var player = _paces[i];
                Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    _rankedDataReceiver?.AddPace(player);
                }, DispatcherPriority.Background);
            }
        }
        else if (receiver is IPlayerAddReceiver playerManagerReceiver)
        {
            _playerManagerReceiver = playerManagerReceiver;
        }
        else if (receiver is IRankedManagementDataReceiver rankedManagementDataReceiver)
        {
            _rankedManagementDataReceiver = rankedManagementDataReceiver;
        }
    }
    public void UnregisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver == _rankedDataReceiver) _rankedDataReceiver = null;
        else if (receiver == _playerManagerReceiver) _playerManagerReceiver = null;
        else if (receiver == _rankedManagementDataReceiver) _rankedManagementDataReceiver = null;
    }

    public async Task Update(CancellationToken token)
    {
        await LoadJsonFileAsync();
        await Task.Delay(TimeSpan.FromSeconds(2), token);
    }
    
    private async Task LoadJsonFileAsync()
    {
        PrivRoomData? privRoomData = null;

        string path = Path.Combine(Consts.AppdataPath, "PrivRoomAPI.json");
        /*
        try
        {
            await using FileStream stream = File.OpenRead(path);
            PrivRoomAPIResult? rankedAPIResult = await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(stream, _options);
            if (rankedAPIResult == null) return;
            privRoomData = rankedAPIResult.Data;
        }
        */
        try
        {
            await using Stream responseStream = await Helper.MakeRequestAsStream($"https://mcsrranked.com/api/users/{TournamentViewModel.RankedApiPlayerName}/live", TournamentViewModel.RankedApiKey);
            PrivRoomAPIResult? rankedAPIResult = await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(responseStream, _options);
            if (rankedAPIResult == null) return;
            privRoomData = rankedAPIResult.Data;
        }
        catch { /**/ }

        if (privRoomData == null) return;

        FilterJSON(privRoomData);
        _rankedDataReceiver?.Update();
        
        _completedRunsCount = privRoomData.Completions.Length;
        _rankedManagementDataReceiver?.UpdateManagementData(_bestSplits.Values.ToList(), _completedRunsCount, _startTime, _paces.Count);
    }
    
    private void FilterJSON(PrivRoomData privRoomData)
    {
        if (privRoomData.Status == _lastStatus && privRoomData.Status != MatchStatus.running) return;
        Console.WriteLine(privRoomData.Status);
        
        //Seed change | New match | just new seed
        if(privRoomData.Status == MatchStatus.generate)
        {
            _lastStatus = privRoomData.Status;
            _startTime = DateTimeOffset.Now.Millisecond;
            Clear();
            return;
        }

        if (privRoomData.Status == MatchStatus.ready || _paces.Count == 0)
        {
            for (int i = 0; i < privRoomData.Players.Length; i++)
            {
                var player = privRoomData.Players[i];
                PrivRoomPaceData data = new() { Player = player };
                
                AddPace(data);
            }
            _lastStatus = privRoomData.Status;
        }
        
        if (privRoomData.Timelines.Count == 0) return;
        for (int i = 0; i < privRoomData.Players.Length; i++)
        {
            var player = privRoomData.Players[i];
            PrivRoomPaceData data = new() { Player = player };

            /*
            privRoomData.Inventories.TryGetValue(player.UUID, out var inventory);
            data.Inventory = inventory!;
            if (data.Inventory.SplashPotions == null) continue;
            */

            for (int j = 0; j < privRoomData.Completions.Length; j++)
            {
                var completion = privRoomData.Completions[j];
                if (!completion.UUID.Equals(player.UUID)) continue;

                data.Completion = completion;
                break;
            }

            for (int j = privRoomData.Timelines.Count - 1; j >= 0; j--)
            {
                var timeline = privRoomData.Timelines[j];
                if (timeline.Type.EndsWith("root"))
                {
                    privRoomData.Timelines.RemoveAt(j);
                    continue;
                }
                if (!timeline.UUID.Equals(player.UUID)) continue;

                if (timeline.Type.EndsWith("reset"))
                {
                    data.Resets++;
                    data.Timelines.Clear();
                    continue;
                }

                data.Timelines.Add(timeline);
                privRoomData.Timelines.RemoveAt(j);
            }

            for (int j = 0; j < _paces.Count; j++)
            {
                var pace = _paces[j];
                if (!pace.InGameName.Equals(data.Player.InGameName)) continue;

                pace.Update(data);
            }
        }
    }
    
    private void AddPace(PrivRoomPaceData data)
    {
        RankedPace pace = new RankedPace(this)
        {
            UUID = data.Player.UUID,
            InGameName = data.Player.InGameName,
            EloRate = data.Player.EloRate ?? -1,
        };
        
        bool found = false;

        var player = TournamentViewModel.GetPlayerByIGN(data.Player.InGameName);
        if (player != null)
        {
            found = true;
            pace.Player = player;
        }
        
        pace.Initialize(data);
        if (!found && TournamentViewModel.AddUnknownRankedPlayersToWhitelist)
        {
            AddRankedPlayerToWhitelist(pace);
        }
        
        _paces.Add(pace);
        _rankedDataReceiver?.AddPace(pace);
    }
    private void AddRankedPlayerToWhitelist(RankedPace pace)
    {
        Player player = new Player()
        {
            UUID = pace.UUID,
            Name = pace.InGameName,
            InGameName = pace.InGameName,
        };

        PlayerViewModel playerViewModel = new PlayerViewModel(player);
        playerViewModel.UpdateHeadImage();

        if (_playerManagerReceiver != null)
        {
            _playerManagerReceiver.Add(playerViewModel);
        }
        else
        {
            TournamentViewModel.AddPlayer(playerViewModel);
        }

        pace.Player = playerViewModel;
    }

    //TODO: 0 przekminic zeby moze pod rankedy z racji oceny graczy po skonczeniu rundy dac liste graczy do api lua
    //czyli zrobic to w formie validowania dopiero po skonczeniu rundy cala lista
    public void EvaluatePlayerInLeaderboard(List<RankedPace> pace, LeaderboardRuleType ruleType)
    {
        
        
    }
    public void EvaluatePlayerInLeaderboard(RankedPace pace)
    {
        if (pace.Player == null) return;
        
        var split = pace.GetLastSplit();
        var milestone = EnumExtensions.FromDescription<RunMilestone>(split.Split.ToString());
        var mainSplit = new LeaderboardTimeline(milestone, (int)split.Time);
        
        var previousSplit = pace.GetSplit(2);
        LeaderboardTimeline? rankedPreviousSplit = null;
        if (previousSplit != null)
        {
            var previousMilestone = EnumExtensions.FromDescription<RunMilestone>(split.Split.ToString());
            rankedPreviousSplit = new LeaderboardTimeline(previousMilestone, (int)previousSplit.Time);
        }
        
        var data = new LeaderboardRankedEvaluateData(pace.Player.Data, mainSplit, rankedPreviousSplit);
        Leaderboard.EvaluatePlayer(data);
    }
    
    public PrivRoomBestSplit GetBestSplit(RankedSplitType splitType)
    {
        _bestSplits.TryGetValue(splitType, out var bestSplit);
        if (bestSplit != null) return bestSplit;

        bestSplit = new PrivRoomBestSplit { Type = splitType };
        _bestSplits.Add(bestSplit.Type, bestSplit);
        _rankedManagementData?.BestSplits.Add(bestSplit);
        return bestSplit;
    }

    private void Clear()
    {
        _bestSplits.Clear();
        _rankedManagementData?.BestSplits.Clear();
        _paces.Clear();
        _rankedManagementDataReceiver?.UpdateManagementData([], 0, 0, 0);
        
        _rankedDataReceiver?.Clear();
    }
}