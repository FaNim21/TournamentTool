using System.Text.Json;
using System.Text.Json.Serialization;
using TournamentTool.Core.Exceptions;
using TournamentTool.Core.Extensions;
using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Parsers;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

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
    public IImageService ImageService { get; }

    private readonly IPlayerViewModelFactory _playerViewModelFactory;
    private readonly IRankedAPIService _rankedApiService;
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly RankedManagementData _rankedManagementData;
    
    private ILeaderboardManager Leaderboard { get; }

    public bool blockSavingPrivRoomData = false;
    public int DelayMiliseconds => 1500;

    private IRankedDataReceiver? _rankedDataReceiver;
    private IRankedManagementDataReceiver? _rankedManagementDataReceiver;
    private IPlayerAddReceiver? _playerManagerReceiver;

    private readonly Settings _settings;

    private Dictionary<RunMilestone, RankedEvaluateTimelineData> _splitDatas = [];
    private Dictionary<RankedSplitType, PrivRoomBestSplit> _bestSplits;
    
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _saveOptions;
    private MatchStatus _lastStatus;
    
    private readonly Dictionary<string, RankedPace> _paces = [];

    private PrivRoomData? _privRoomData;

    private int _lastEvaluatedRoomID = -1;
    
    
    public RankedService(ILeaderboardManager leaderboard, ILoggingService logger, ISettingsProvider settingsProvider, IPlayerViewModelFactory playerViewModelFactory, 
        IRankedAPIService rankedApiService, IImageService imageService, ITournamentState tournamentState, ITournamentPlayerRepository playerRepository)
    {
        Logger = logger;
        ImageService = imageService;
        Leaderboard = leaderboard;
        _playerViewModelFactory = playerViewModelFactory;
        _rankedApiService = rankedApiService;
        _tournamentState = tournamentState;
        _playerRepository = playerRepository;
        
        _settings = settingsProvider.Get<Settings>();

        _rankedManagementData = (tournamentState.CurrentPreset.ManagementData as RankedManagementData)!;
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
            _rankedDataReceiver.AddPaces(_paces.Values);
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
        if (string.IsNullOrWhiteSpace(_tournamentState.CurrentPreset.RankedApiKey))
            throw new BackgroundServiceException("Ranked API key is empty");

        if (string.IsNullOrWhiteSpace(_tournamentState.CurrentPreset.RankedApiPlayerName))
            throw new BackgroundServiceException("Player name for API is empty");
        
        await LoadJsonFileAsync();
    }
    
    private async Task LoadJsonFileAsync()
    {
        PrivRoomAPIResult? rankedAPIResult = await _rankedApiService.GetRankedPrivateRoomLiveData(_tournamentState.CurrentPreset.RankedApiPlayerName, _tournamentState.CurrentPreset.RankedApiKey);
        _privRoomData = rankedAPIResult?.Data;

        if (_privRoomData == null)
            throw new BackgroundServiceException("Problem with data requested from api (check notification panel in status bar (bell) or console for more informations)");

        FilterJSON(_privRoomData);
        _rankedDataReceiver?.Update();
        
        _rankedManagementData!.Completions = _privRoomData.Completions.Length;
        _rankedManagementData!.Players = _privRoomData.Players.Length;
        _rankedManagementDataReceiver?.Update();
    }
    
    public void FilterJSON(PrivRoomData privRoomData)
    {
        if (privRoomData.Status == _lastStatus && privRoomData.Status != MatchStatus.running) return;
        
        if(privRoomData.Status == MatchStatus.done)
        {
            _lastStatus = privRoomData.Status;
            UpdateTimelineInPaces(privRoomData.Timelines);
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
        
        UpdateTimelineInPaces(privRoomData.Timelines);
        UpdatePlayersInventory();
    }

    private void UpdateTimelineInPaces(PrivRoomTimeline[] timelines)
    {
        if (timelines.Length == 0) return;
        
        for (int i = timelines.Length - 1; i >= 0; i--)
        {
            var timeline = timelines[i];
            if (timeline.Type.EndsWith("root")) continue;
            
            RunMilestoneData parsedTimeline = RunMilestoneParser.Parse(timeline.Type);
            var time = timeline.Time;
            var paceTimeline = new RankedPaceTimeline(parsedTimeline.Name, parsedTimeline.Milestone, time);

            if (!_paces.TryGetValue(timeline.UUID, out var pace))
            {
                pace = AddPace(timeline.UUID);
            }
            pace.AddTimeline(paceTimeline);
        }
    }
    private void UpdatePlayersInventory()
    {
        foreach (var pace in _paces)
        {
            //TODO: 5 TUTAJ INVENTORY JAK DODADZA DO API
            pace.Value.Update(null!);
        }
    }

    private void AddPace(PrivRoomPlayer player) => AddPace(player.UUID, player.InGameName);
    private RankedPace AddPace(string UUID, string ign = "")
    {
        Player? data = _playerRepository.GetPlayerByUUID(UUID)?.Data;
        string inGameName = data != null ? data.InGameName ?? ign : ign;
        
        RankedPace pace = new RankedPace(this, UUID, inGameName, data);
        _paces[UUID] = pace;
        
        pace.Initialize();
        if (pace.Player == null && _tournamentState.CurrentPreset.AddUnknownRankedPlayersToWhitelist)
        {
            AddRankedPlayerToWhitelist(pace);
        }
        
        _rankedDataReceiver?.AddPace(pace);
        return pace;
    }
    private void AddRankedPlayerToWhitelist(RankedPace pace)
    {
        Player player = new Player()
        {
            UUID = pace.UUID,
            Name = pace.InGameName,
            InGameName = pace.InGameName,
        };

        IPlayerViewModel playerViewModel = _playerViewModelFactory.Create(player);
        playerViewModel.UpdateHeadImage();

        if (_playerManagerReceiver != null)
        {
            _playerManagerReceiver.Add(playerViewModel);
        }
        else
        {
            _playerRepository.AddPlayer(playerViewModel);
        }

        pace.Player = player;
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
            string fileName = $"{logName} {date}.json";

            int count = 1;
            while (File.Exists(Consts.AppdataPath + "\\" + fileName))
            {
                fileName = $"{logName} {date} [{count}].json";
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
        int dataLastID = data.LastID ?? 1;
        if (_lastEvaluatedRoomID == dataLastID) return;
        _lastEvaluatedRoomID = dataLastID;
        
        int completions = data.Completions.Length;
        var display = RunMilestone.ProjectEloComplete.GetDisplay()!;

        if (_settings.SaveRankedPrivRoomDataOnSeedFinish && !blockSavingPrivRoomData)
        {
            SavePrivRoomFinishedData();
        }
        
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
        _lastEvaluatedRoomID = -1;
        Clear();
    }
    public void ReadySeed(PrivRoomData privRoomData)
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

    public string GetHeadURL(string id)
    {
        return _settings.HeadAPIType.GetHeadURL(id, 8);
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
        _rankedManagementData.RefreshUI = true;
        _rankedManagementData!.BestSplitsDatas.Clear();
        _rankedManagementData.Completions = 0;
        _rankedManagementData.Players = 0;
        _rankedManagementData.StartTime = 0;
        
        _rankedDataReceiver?.Clear();
    }
}