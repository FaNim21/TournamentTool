using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using TournamentTool.Enums;
using TournamentTool.Extensions;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Logging;
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
    private ILoggingService Logger { get; }
    public ISettings SettingsService { get; }
    private readonly RankedManagementData _rankedManagementData;
    
    private TournamentViewModel TournamentViewModel { get; }
    private ILeaderboardManager Leaderboard { get; }

    private IRankedDataReceiver? _rankedDataReceiver;
    private IRankedManagementDataReceiver? _rankedManagementDataReceiver;
    private IPlayerAddReceiver? _playerManagerReceiver;

    private Dictionary<RunMilestone, RankedEvaluateTimelineData> _splitDatas = [];
    private Dictionary<RankedSplitType, PrivRoomBestSplit> _bestSplits;
    
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _saveOptions;
    private MatchStatus _lastStatus;
    
    private readonly Dictionary<string, RankedPace> _paces = [];

    private const int UiSendBatchSize = 7;
    
    PrivRoomData? _privRoomData;
    
    
    public RankedService(TournamentViewModel tournamentViewModel, ILeaderboardManager leaderboard, ILoggingService logger, ISettings settingsService)
    {
        Logger = logger;
        SettingsService = settingsService;
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
        _rankedManagementData = (TournamentViewModel.ManagementData as RankedManagementData)!;
        _bestSplits = _rankedManagementData.BestSplitsDatas.ToDictionary(b => b.Type, b => b) ?? [];

        _options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            PropertyNameCaseInsensitive = true
        };
        _saveOptions = new JsonSerializerOptions() { WriteIndented = true };
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
        await Task.Delay(TimeSpan.FromMilliseconds(1500), token);
    }
    
    private async Task LoadJsonFileAsync()
    {

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
            _privRoomData = rankedAPIResult.Data;
        }
        catch { /**/ }

        if (_privRoomData == null) return;

        FilterJSON(_privRoomData);
        _rankedDataReceiver?.Update();
        
        _rankedManagementData!.Completions = _privRoomData.Completions.Length;
        _rankedManagementData!.Players = _privRoomData.Players.Length;
        _rankedManagementDataReceiver?.Update();
    }
    
    private void FilterJSON(PrivRoomData privRoomData)
    {
        if (privRoomData.Status == _lastStatus && privRoomData.Status != MatchStatus.running) return;
        
        if(privRoomData.Status == MatchStatus.done)
        {
            _lastStatus = privRoomData.Status;
            EvaluateResults(privRoomData);
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
            //TODO: 5 TUTAJ INVENTORY JAK DODADZA DO API
            pace.Value.Update(null!);
        }
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
        
        string url = SettingsService.Settings.HeadAPIType.GetHeadURL(player.UUID, 32);
        playerViewModel.UpdateHeadImage(url);

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
        
        var data = new LeaderboardRankedEvaluateData(player, _rankedManagementData.Rounds, mainTimeline, previousTimeline);
        evaluateTimelineData.Add(data);
    }

    private void SavePrivRoomFinishedData()
    {
        if (_privRoomData == null) return;

        try
        {
            var rankedSaveData = JsonSerializer.Serialize(_privRoomData, _saveOptions);

            string logName = $"PrivRoomData({_privRoomData.Completions.Length})";
            string date = DateTimeOffset.Now.ToString("yyyy-MM-dd_HH.mm");
            string fileName = $"{logName} {date}.txt";

            int count = 1;
            while (File.Exists(Consts.LogsPath + "\\" + fileName))
            {
                fileName = $"{logName} {date} [{count}].txt";
                count++;
            }

            File.WriteAllText(Consts.AppdataPath + "\\" + fileName, rankedSaveData);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }
    private void EvaluateResults(PrivRoomData data)
    {
        int completions = data.Completions.Length;
        var display = RunMilestone.ProjectEloComplete.GetDisplay()!;
        
        for (int i = 0; i < data.Completions.Length; i++)
        {
            var completion = data.Completions[i];

            if (!_paces.TryGetValue(completion.UUID, out var paceData)) continue;
            if (paceData.GetLastSplit().Split == RankedSplitType.complete) continue;

            var paceTimeline = new RankedPaceTimeline(display.ShortName!, RunMilestone.ProjectEloComplete, completion.Time);
            paceData.AddTimeline(paceTimeline);
        }
        
        if (_splitDatas.Count == 0 || completions == 0) return;

        Leaderboard.EvaluateData(_splitDatas);
        _rankedManagementData.Rounds++;
    }
    private void SeedStarted()
    {
        //Seed change | New match
        _rankedManagementData.StartTime = DateTimeOffset.Now.Millisecond;
        Clear();
    }
    private void ReadySeed(PrivRoomData privRoomData)
    {
        if (_rankedManagementData!.BestSplitsDatas.Count != 0)
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
        _rankedManagementData?.BestSplitsDatas.Add(bestSplit);
        return bestSplit;
    }
    
    private void Clear()
    {
        _bestSplits.Clear();
        _paces.Clear();

        for (int i = 0; i < _rankedManagementData.BestSplitsDatas.Count; i++)
        {
            var currentSplit = _rankedManagementData.BestSplitsDatas[i];
            currentSplit.Datas.Clear();
        }
        _rankedManagementData!.BestSplitsDatas.Clear();
        _rankedManagementData.Completions = 0;
        _rankedManagementData.Players = 0;
        _rankedManagementData.StartTime = 0;
        
        _rankedDataReceiver?.Clear();
    }
}