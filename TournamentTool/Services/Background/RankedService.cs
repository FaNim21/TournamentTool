using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using MethodTimer;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services.Background;

public class RankedEvaluateTimelineData
{
    public readonly List<LeaderboardRankedEvaluateData> Evaluations = [];

    public void Add(LeaderboardRankedEvaluateData data)
    {
        if (data == null) return;
        
        var duplicate = Evaluations.FirstOrDefault(e => e.Player.InGameName == data.Player.InGameName);
        if (duplicate != null) return;
        
        Evaluations.Add(data);
    }
    public void Remove(LeaderboardRankedEvaluateData data)
    {
        Evaluations.Remove(data);
    }
}

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

    private Dictionary<RunMilestone, RankedEvaluateTimelineData> _splitDatas = [];
    
    private List<RankedPace> _paces = [];
    private Dictionary<RankedSplitType, PrivRoomBestSplit> _bestSplits;
    
    private readonly JsonSerializerOptions _options;
    private MatchStatus _lastStatus;
    private int _round;
    
    private readonly Dictionary<string, PrivRoomPaceData> _privRoomPaceDatas = [];
    private readonly Dictionary<string, (string name, RunMilestone milestone)> _timelineTypeCache = new();

    private const int UiSendBatchSize = 10;
    
    
    //TODO: 0 w UI przy dodawaniu subrule pokazac czy sie jest na rankedzie czy pacemanie i wtedy moze ewentualnie inna zawartosc dawac
    public RankedService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
        _rankedManagementData = TournamentViewModel.ManagementData as RankedManagementData;
        _round = _rankedManagementData!.Rounds;
        _bestSplits = _rankedManagementData.BestSplits.ToDictionary(b => b.Type, b => b) ?? [];

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
            foreach (var batch in _paces.Batch(UiSendBatchSize))
            {
                Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    foreach (var player in batch)
                    {
                        _rankedDataReceiver?.AddPace(player);
                    }
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
        if (string.IsNullOrWhiteSpace(TournamentViewModel.RankedApiKey) ||
            string.IsNullOrWhiteSpace(TournamentViewModel.RankedApiPlayerName)) return;
        
        await LoadJsonFileAsync();
        await Task.Delay(TimeSpan.FromSeconds(2), token);
    }
    
    private async Task LoadJsonFileAsync()
    {
        PrivRoomData? privRoomData = null;

        string path = Path.Combine(Consts.AppdataPath, "PrivRoomAPIHUGE.json");
        //api huge potrzebuje chodzenia po timeline'ach w normalnej kolejnosci, bo to stare api
        try
        {
            await using FileStream stream = File.OpenRead(path);
            PrivRoomAPIResult? rankedAPIResult = await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(stream, _options);
            if (rankedAPIResult == null) return;
            privRoomData = rankedAPIResult.Data;
        }
        /*
        try
        {
            await using Stream responseStream = await Helper.MakeRequestAsStream($"https://mcsrranked.com/api/users/{TournamentViewModel.RankedApiPlayerName}/live", TournamentViewModel.RankedApiKey);
            PrivRoomAPIResult? rankedAPIResult = await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(responseStream, _options);
            if (rankedAPIResult == null) return;
            privRoomData = rankedAPIResult.Data;
        }
        */
        catch { /**/ }

        if (privRoomData == null) return;

        FilterJSON(privRoomData);
        _rankedDataReceiver?.Update();
        
        _completedRunsCount = privRoomData.Completions.Length;
        _rankedManagementDataReceiver?.UpdateManagementData(_bestSplits.Values.ToList(), _completedRunsCount, _startTime, _paces.Count, _round);
    }
    
    private void FilterJSON(PrivRoomData privRoomData)
    {
        if (privRoomData.Status == _lastStatus && privRoomData.Status != MatchStatus.running) return;
        Console.WriteLine(privRoomData.Status);
        
        if(privRoomData.Status == MatchStatus.done)
        {
            _lastStatus = privRoomData.Status;
            EvaluateResults();
            return;
        }
        if(privRoomData.Status == MatchStatus.generate)
        {
            _lastStatus = privRoomData.Status;
            SeedStarted();
            return;
        }
        if (privRoomData.Status == MatchStatus.ready || _paces.Count == 0)
        {
            _lastStatus = privRoomData.Status;
            ReadySeed(privRoomData);
        }
        
        if (privRoomData.Timelines.Length == 0) return;
        
        for (int i = 0; i < privRoomData.Timelines.Length; i++)
        {
            var timeline = privRoomData.Timelines[i];
            if (timeline.Type.EndsWith("root")) continue;
            
            (string name, RunMilestone milestone) parsedTimeline = ParseTimelineType(timeline.Type);
            var time = timeline.Time;
            var paceTimeline = new RankedPaceTimeline(parsedTimeline.name, parsedTimeline.milestone, time);
            
            if (!_privRoomPaceDatas.TryGetValue(timeline.UUID, out var paceData)) continue;
            paceData.Timelines.Add(paceTimeline);
            
            AddTimelineToEvaluation(paceData);

            if (paceTimeline.Milestone != RunMilestone.ProjectEloReset) continue;
            paceData.Resets++;
            paceData.Timelines.Clear();
        }

        for (int i = 0; i < privRoomData.Completions.Length; i++)
        {
            var completion = privRoomData.Completions[i];

            if (!_privRoomPaceDatas.TryGetValue(completion.UUID, out var paceData)) continue;
            if (paceData.Completion != null) continue;
            
            paceData.Completion = completion;
        }

        for (int i = 0; i < _paces.Count; i++)
        {
            var pace = _paces[i];
            
            //tu by tez byloby podpinanie inventory jak juz bedzie

            if (_privRoomPaceDatas.TryGetValue(pace.UUID, out var paceData))
                pace.Update(paceData);
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
        if (data.WhitelistPlayer != null)
        {
            found = true;
            pace.Player = data.WhitelistPlayer;
        }
        
        pace.Initialize(data);
        if (!found && TournamentViewModel.AddUnknownRankedPlayersToWhitelist)
        {
            AddRankedPlayerToWhitelist(pace, data);
        }
        
        _paces.Add(pace);
        _rankedDataReceiver?.AddPace(pace);
    }
    private void AddRankedPlayerToWhitelist(RankedPace pace, PrivRoomPaceData data)
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
        data.WhitelistPlayer = playerViewModel;
    }

    public void AddEvaluationData(Player player, RankedPaceTimeline main, RankedPaceTimeline? previous = null)
    {
        if (!_splitDatas.TryGetValue(main.Milestone, out var evaluateTimelineData))
        {
            evaluateTimelineData = new RankedEvaluateTimelineData();
            _splitDatas[main.Milestone] = evaluateTimelineData;
        }

        LeaderboardTimeline mainTimeline = new LeaderboardTimeline(main.Milestone, (int)main.Time);
        LeaderboardTimeline? previousTimeline = null;
        if (previous != null)
        {
            previousTimeline = new LeaderboardTimeline(previous.Milestone, (int)previous.Time);
        }
        
        var data = new LeaderboardRankedEvaluateData(player, mainTimeline, previousTimeline);
        evaluateTimelineData.Add(data);
    }

    private void EvaluateResults()
    {
        if (_splitDatas.Count == 0) return;
        
        _round++;
        Leaderboard.EvaluateData(_splitDatas);
    }
    private void SeedStarted()
    {
        //Seed change | New match
        _startTime = DateTimeOffset.Now.Millisecond;
        Clear();
    }
    private void ReadySeed(PrivRoomData privRoomData)
    {
        if (_rankedManagementData!.BestSplits.Count != 0)
        {
            _startTime = DateTimeOffset.Now.Millisecond - privRoomData.Time;
        }
        
        //Counting after seed loaded
        for (int i = 0; i < privRoomData.Players.Length; i++)
        {
            var player = privRoomData.Players[i];
            
            PrivRoomPaceData data = new()
            {
                Player = player,
                WhitelistPlayer = TournamentViewModel.GetPlayerByIGN(player.InGameName)
            };

            _privRoomPaceDatas[player.UUID] = data;
            AddPace(data);
        }
    }

    private void AddTimelineToEvaluation(PrivRoomPaceData paceData)
    {
        RankedPaceTimeline mainTimeline = paceData.Timelines[^1];
        ValidateBestSplit(mainTimeline);
        
        RankedPaceTimeline? previousTimeline = null;
        if (paceData.Timelines.Count > 2)
        {
            previousTimeline = paceData.Timelines[^2];
        }

        if (paceData.WhitelistPlayer == null) return;
        AddEvaluationData(paceData.WhitelistPlayer.Data, mainTimeline, previousTimeline);
    }

    private void ValidateBestSplit(RankedPaceTimeline timeline)
    {
        /*
        PrivRoomBestSplit bestSplit = GetBestSplit(newSplit.Split);
        
        if(string.IsNullOrEmpty(bestSplit.PlayerName))
        {
            bestSplit.PlayerName = InGameName;
            bestSplit.Time = newSplit.Time;
        }
        DifferenceSplitTimeMiliseconds = newSplit.Time - bestSplit.Time;
        if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;
    */
    }

    public PrivRoomBestSplit GetBestSplit(RankedSplitType splitType)
    {
        _bestSplits.TryGetValue(splitType, out var bestSplit);
        if (bestSplit != null) return bestSplit;

        bestSplit = new PrivRoomBestSplit { Type = splitType };
        _bestSplits[bestSplit.Type] = bestSplit;
        _rankedManagementData?.BestSplits.Add(bestSplit);
        return bestSplit;
    }
    
    private (string name, RunMilestone milestone) ParseTimelineType(string type)
    {
        if (_timelineTypeCache.TryGetValue(type, out var cached)) return cached;
    
        var name = type.Split('.')[^1];
        var milestone = EnumExtensions.FromDescription<RunMilestone>(type);
        var result = (name, milestone);
    
        _timelineTypeCache[type] = result;
        return result;
    }

    private void Clear()
    {
        _bestSplits.Clear();
        _rankedManagementData?.BestSplits.Clear();
        _paces.Clear();
        _rankedManagementDataReceiver?.UpdateManagementData([], 0, 0, 0, _rankedManagementData!.Rounds);
        
        _rankedDataReceiver?.Clear();
        _privRoomPaceDatas.Clear();
    }
}