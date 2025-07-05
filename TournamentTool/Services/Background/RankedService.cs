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
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;
using TournamentTool.Utils.Parsers;
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
    private readonly RankedManagementData _rankedManagementData;
    
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }

    private IRankedDataReceiver? _rankedDataReceiver;
    private IRankedManagementDataReceiver? _rankedManagementDataReceiver;
    private IPlayerAddReceiver? _playerManagerReceiver;

    private Dictionary<RunMilestone, RankedEvaluateTimelineData> _splitDatas = [];
    private Dictionary<RankedSplitType, PrivRoomBestSplit> _bestSplits;
    
    private readonly JsonSerializerOptions _options;
    private MatchStatus _lastStatus;
    
    private readonly Dictionary<string, RankedPace> _paces = [];

    private const int UiSendBatchSize = 7;
    
    
    //TODO: 0 w UI przy dodawaniu subrule pokazac czy sie jest na rankedzie czy pacemanie i wtedy moze ewentualnie inna zawartosc dawac
    public RankedService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
        _rankedManagementData = (TournamentViewModel.ManagementData as RankedManagementData)!;
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
            List<RankedPace> paces = _paces.Values.ToList();
            foreach (var batch in paces.Batch(UiSendBatchSize))
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

        //api huge potrzebuje chodzenia po timeline'ach w normalnej kolejnosci, bo to stare api
        /*
        string path = Path.Combine(Consts.AppdataPath, "PrivRoomAPIHUGE.json");
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
        
        _rankedManagementData!.Completions = privRoomData.Completions.Length;
        _rankedManagementData!.Players = privRoomData.Players.Length;
        _rankedManagementDataReceiver?.Update();
    }
    
    private void FilterJSON(PrivRoomData privRoomData)
    {
        if (privRoomData.Status == _lastStatus && privRoomData.Status != MatchStatus.running) return;
        
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
        
        for (int i = privRoomData.Timelines.Length - 1; i >= 0; i--)
        {
            var timeline = privRoomData.Timelines[i];
            if (timeline.Type.EndsWith("root")) continue;
            
            RunMilestoneData parsedTimeline = RunMilestoneParser.Parse(timeline.Type);
            var time = timeline.Time;
            var paceTimeline = new RankedPaceTimeline(parsedTimeline.Name, parsedTimeline.Milestone, time);
            
            if (!_paces.TryGetValue(timeline.UUID, out var pace)) continue;
            pace.AddTimeline(paceTimeline);
        }

        foreach (var pace in _paces)
        {
            //TODO: 0 TUTAJ INVENTORY JAK DODADZA DO API
            pace.Value.Update(null);
        }
        
        //TODO: 0 Z tym tez cos zrobic jezeli nie naprawia timeline'ow dla defaultowego completionw priv roomie,
        //bo wtedy na bazie tego trzeba bedzie samemu dorobic ten timeline
        /*
        for (int i = 0; i < privRoomData.Completions.Length; i++)
        {
            var completion = privRoomData.Completions[i];

            if (!_paces.TryGetValue(completion.UUID, out var paceData)) continue;
            if (paceData.Completion != null) continue;

            paceData.Completion = completion;
        }
        */
    }

    private void AddPace(PrivRoomPlayer player)
    {
        RankedPace pace = new RankedPace(this)
        {
            UUID = player.UUID,
            InGameName = player.InGameName,
            EloRate = player.EloRate ?? -1,
            Player = TournamentViewModel.GetPlayerByIGN(player.InGameName)
        };
            
        _paces[player.UUID] = pace;
        
        pace.Initialize();
        if (pace.Player == null && TournamentViewModel.AddUnknownRankedPlayersToWhitelist)
        {
            AddRankedPlayerToWhitelist(pace);
        }
        
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

        _rankedManagementData.Rounds++;
        Leaderboard.EvaluateData(_splitDatas);
    }
    private void SeedStarted()
    {
        //Seed change | New match
        _rankedManagementData.StartTime = DateTimeOffset.Now.Millisecond;
        Clear();
    }
    private void ReadySeed(PrivRoomData privRoomData)
    {
        if (_rankedManagementData!.BestSplits.Count != 0)
        {
            _rankedManagementData.StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - privRoomData.Time;
        }
        
        //Counting after seed loaded
        for (int i = 0; i < privRoomData.Players.Length; i++)
        {
            var player = privRoomData.Players[i];
            AddPace(player);
        }
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
    
    private void Clear()
    {
        _bestSplits.Clear();
        _paces.Clear();
        
        _rankedManagementData!.BestSplits.Clear();
        _rankedManagementData.Completions = 0;
        _rankedManagementData.Players = 0;
        _rankedManagementData.StartTime = 0;
        
        _rankedDataReceiver?.Clear();
    }
}