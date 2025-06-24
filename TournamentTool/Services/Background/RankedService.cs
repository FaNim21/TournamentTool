using System.IO;
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
    private Dictionary<RankedSplitType, RankedBestSplit> _bestSplits = [];
    
    private readonly JsonSerializerOptions _options;
    private readonly string _filePath;
    
    
    public RankedService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
        _rankedManagementData = TournamentViewModel.ManagementData as RankedManagementData;
        
        string dataName = TournamentViewModel.RankedRoomDataName;
        if(!dataName.EndsWith(".json"))
        {
            dataName = Path.Combine(dataName, ".json");
        }
        _filePath = Path.Combine(TournamentViewModel.RankedRoomDataPath, dataName);

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
            Application.Current.Dispatcher.InvokeAsync(() => 
            {
                _rankedDataReceiver?.ReceiveAllPaces(_paces);
            }, _paces.Count > 25 ? DispatcherPriority.Background : DispatcherPriority.Normal);
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
        await Task.Delay(TimeSpan.FromMilliseconds(TournamentViewModel.RankedRoomUpdateFrequency), token);
    }
    
    private async Task LoadJsonFileAsync()
    {
        RankedData? rankedData = null;

        try
        {
            await using FileStream stream = File.OpenRead(_filePath);
            rankedData = await JsonSerializer.DeserializeAsync<RankedData>(stream, _options);
        }
        catch { /**/ }

        if (rankedData == null) return;

        FilterJSON(rankedData);
        _rankedDataReceiver?.Update();
        
        _completedRunsCount = rankedData.Completes.Length;
        _rankedManagementDataReceiver?.UpdateManagementData(_bestSplits.Values.ToList(), _completedRunsCount, _startTime, _paces.Count);
    }
    
    private void FilterJSON(RankedData rankedData)
    {
        if (rankedData.Timelines.Count == 0)
        {
            _paces.Clear();
        }

        //Seed change | New match | just new seed
        //Console.WriteLine($"{rankedData.StartTime} ------ {_startTime}");
        if(rankedData.StartTime != _startTime)
        {
            _startTime = rankedData.StartTime;
            Clear();
        }

        List<RankedPace> _currentPaces = new(_paces);
        for (int i = 0; i < rankedData.Players.Length; i++)
        {
            var player = rankedData.Players[i];
            RankedPaceData data = new()
            {
                Player = player,
                Timelines = []
            };

            rankedData.Inventories.TryGetValue(player.UUID, out var inventory);
            data.Inventory = inventory!;
            if (data.Inventory.SplashPotions == null) continue;

            for (int j = 0; j < rankedData.Completes.Length; j++)
            {
                var completion = rankedData.Completes[j];
                if (!completion.UUID.Equals(player.UUID)) continue;

                data.Completion = completion;
                break;
            }

            for (int j = 0; j < rankedData.Timelines.Count; j++)
            {
                var timeline = rankedData.Timelines[j];
                if (timeline.Type.EndsWith("root")) continue;
                if (!timeline.UUID.Equals(player.UUID)) continue;

                if (timeline.Type.EndsWith("reset"))
                {
                    data.Resets++;
                    data.Timelines.Clear();
                    continue;
                }
                timeline.Type = timeline.Type.Split('.')[^1];

                data.Timelines.Add(timeline);
                rankedData.Timelines.RemoveAt(j);
                j--;
            }

            bool wasFoundOnPaces = false;
            for (int j = 0; j < _currentPaces.Count; j++)
            {
                var pace = _currentPaces[j];
                if (!pace.InGameName.Equals(data.Player.NickName)) continue;

                Application.Current.Dispatcher.Invoke(() => { pace.Update(data); });
                wasFoundOnPaces = true;
                _currentPaces.Remove(pace);
                break;
            }

            if (!wasFoundOnPaces) AddPace(data);
        }

        for (int i = 0; i < _currentPaces.Count; i++)
        {
            RemovePace(_currentPaces[i]);
        }
    }
    
    private void AddPace(RankedPaceData data)
    {
        RankedPace pace = new RankedPace(this)
        {
            UUID = data.Player.UUID,
            InGameName = data.Player.NickName,
            EloRate = data.Player.EloRate ?? -1,
        };
        
        bool found = false;

        var player = TournamentViewModel.GetPlayerByIGN(data.Player.NickName);
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
    private void RemovePace(RankedPace pace)
    {
        _paces.Remove(pace);
        _rankedDataReceiver?.RemovePace(pace);
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
    
    public RankedBestSplit GetBestSplit(RankedSplitType splitType)
    {
        _bestSplits.TryGetValue(splitType, out var bestSplit);
        if (bestSplit != null) return bestSplit;

        bestSplit = new RankedBestSplit { Type = splitType };
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