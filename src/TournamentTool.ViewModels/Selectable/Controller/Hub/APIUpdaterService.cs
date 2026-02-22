using OBSStudioClient.Classes;
using TournamentTool.Core.Extensions;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.ViewModels.Selectable.Controller.Hub;

public class APIUpdaterService : IServiceUpdater
{
    private ILoggingService Logger { get; }
    
    private readonly ControllerViewModel _controller;
    private readonly ObsController _obs;
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentLeaderboardRepository _leaderboardRepository;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly APIDataSaver _api;

    private record LeaderboardPlayerAPINames(string IGN, string PreviousRoundPoints, string OverallPoints);
    private record SplitsTwoPlayersAPINames(string Time, string TimeDifference);

    private List<LeaderboardPlayerAPINames> _leaderboardAPINames = [];
    private List<SplitsTwoPlayersAPINames>[] _splitsAPINames = [];
    
    private string[] _rankedSplits = ["enter_the_nether", "structure_1", "structure_2", 
        "blind_travel", "follow_ender_eye", "enter_the_end", "kill_dragon", "complete"];

    private string[] _rankedNames = ["1_split_player_name", "2_split_player_name", "3_split_player_name"];
    private string[] _savedNames = new string[3];
    
    
    public APIUpdaterService(ControllerViewModel controller, ILoggingService logger, ObsController obs, ITournamentState tournamentState, 
        ITournamentLeaderboardRepository leaderboardRepository, ITournamentPlayerRepository playerRepository)
    {
        Logger = logger;
        _controller = controller;
        _obs = obs;
        _tournamentState = tournamentState;
        _leaderboardRepository = leaderboardRepository;
        _playerRepository = playerRepository;

        _api = new APIDataSaver();
        
        try
        {
            LeaderboardAPICheck();
            UpdateTwoPlayersAPICheck();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    public void OnEnable()
    {
        _controller.ManagementPanel?.InitializeAPI(_api);
    }
    public void OnDisable()
    {
        
    }

    public async Task UpdateAsync(CancellationToken token)
    {
        if (_controller.ManagementPanel != null)
        {
            await _controller.ManagementPanel.UpdateAPI(_api);
        }
        
        try
        {
            await UpdateLeaderboardTopAPI();
            await UpdateTwoPlayersSplits();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    /// <summary>
    /// HARD CODED Rozwiazanie tymczasowe na potrzebe KOSTU polskiej ligi
    /// </summary>
    private void UpdateTwoPlayersAPICheck()
    {
        _splitsAPINames = new List<SplitsTwoPlayersAPINames>[3];
        _splitsAPINames[0] = [];
        _splitsAPINames[1] = [];
        _splitsAPINames[2] = [];
        
        for (int i = 0; i < 3; i++)
        {
            int number = i + 1;
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                string currentSplit = _rankedSplits[j];
                _splitsAPINames[i].Add(new SplitsTwoPlayersAPINames($"{number}_{currentSplit}_Time", 
                    $"{number}_{currentSplit}_Difference"));
            }
        }
    }
    private async Task UpdateTwoPlayersSplits()
    {
        if (_tournamentState.CurrentPreset.ManagementData is not RankedManagementData rankedManagementData) return;
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                await _api.UpdateFileContent(_splitsAPINames[i][j].Time, string.Empty);
                await _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
            }
            await _api.UpdateFileContent(_rankedNames[i], string.Empty);
        }
        
        SceneItem[] itemsInHeadsGroup = [];
        List<string> sourceNames = [];

        try
        {
            itemsInHeadsGroup = await _obs.GetGroupSceneItemList("splits_heads");
        }
        catch (Exception ex)
        {
            // Logger.Error(ex);
        }

        for (int i = 0; i < itemsInHeadsGroup.Length; i++)
        {
            var item = itemsInHeadsGroup[i];
            var sourceName = item.SourceName.ToLower();
            if (!sourceName.StartsWith("split_head")) continue;

            string numberLetters = sourceName[^2..];
            var digits = new string(numberLetters.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out int number)) continue;
            
            number -= 1;
            if (number is < 0 or > 2) continue;
            sourceNames.Add(item.SourceName);
        }

        for (var i = 0; i < sourceNames.Count; i++)
        {
            var sourceName = sourceNames[i];
            _obs.SetBrowserURL(sourceName, string.Empty);
            _savedNames[i] = string.Empty;
        }

        for (var i = 0; i < sourceNames.Count; i++)
        {
            var sourceName = sourceNames[i];
            
            if (rankedManagementData.BestSplitsDatas.Count == 0) return;
            if (i >= rankedManagementData.BestSplitsDatas[0].Datas.Count)
            {
                _obs.SetBrowserURL(sourceName, string.Empty);
                _savedNames[i] = string.Empty;
                continue;
            }

            if (i >= rankedManagementData.BestSplitsDatas[0].Datas.Count)
            {
                _obs.SetBrowserURL(sourceName, string.Empty);
                _savedNames[i] = string.Empty;
                await _api.UpdateFileContent(_rankedNames[i], _savedNames[i]);
                continue;
            }

            var data = rankedManagementData.BestSplitsDatas[0].Datas[i];

            string url = _controller.GetHeadURL(data.PlayerName, 180);
            _obs.SetBrowserURL(sourceName, url);
            _savedNames[i] = data.PlayerName;
            await _api.UpdateFileContent(_rankedNames[i], _savedNames[i]);
        }
        
        UpdateTwoPlayersSplitsData(rankedManagementData);
    }
    private void UpdateTwoPlayersSplitsData(RankedManagementData rankedManagementData)
    {
        List<List<long>> playersTimes = [];
        
        for (int i = 0; i < 3; i++)
        {
            playersTimes.Add([]);
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                PrivRoomBestSplit? currentSplit = null;
                for (int k = 0; k < rankedManagementData.BestSplitsDatas.Count; k++)
                {
                    var best = rankedManagementData.BestSplitsDatas[k];
                    if (!best.Type.ToString().Equals(_rankedSplits[j])) continue;
                    
                    currentSplit = rankedManagementData.BestSplitsDatas[k];
                    break;
                }
                bool foundData = false;

                if (currentSplit == null) continue;
                for (int k = 0; k < currentSplit.Datas.Count; k++)
                {
                    var data = currentSplit.Datas[k];
                    if (!data.PlayerName.Equals(_savedNames[i])) continue;
                    
                    var formattedTime = TimeSpan.FromMilliseconds(data.Time).ToFormattedTime();
                    _api.UpdateFileContent(_splitsAPINames[i][j].Time, formattedTime);
                    
                    playersTimes[i].Add(data.Time);
                    foundData = true;
                    break;
                }

                if (foundData) continue;
                playersTimes[i].Add(0);
                _api.UpdateFileContent(_splitsAPINames[i][j].Time, string.Empty);
                _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
            }
        }
        
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                if (j >= playersTimes[i].Count || playersTimes[i][j] == 0)
                {
                    _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
                    continue;
                }

                int fastestPlayerIndex = -1;
                long fastestTime = long.MaxValue;
                for (int k = 0; k < playersTimes.Count; k++)
                {
                    if (j >= playersTimes[k].Count || playersTimes[k][j] == 0) continue;
                    
                    long currentTime = playersTimes[k][j];
                    if (fastestTime < currentTime) continue;
                    
                    fastestPlayerIndex = k;
                    fastestTime = currentTime;
                }

                if (fastestPlayerIndex == -1 || fastestPlayerIndex == i)
                {
                    _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
                    continue;
                }

                long difference = fastestTime - playersTimes[i][j];
                string formattedTime = TimeSpan.FromMilliseconds(Math.Abs(difference)).ToFormattedTime();
                
                if (difference < 0)
                {
                    formattedTime = "+" + formattedTime;
                }
                else
                {
                    formattedTime = "-" + formattedTime;
                }

                _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, formattedTime);
            }
        }
    }

    /// <summary>
    /// HARD CODED Rozwiazanie tymczasowe na potrzebe KOSTU polskiej ligi
    /// </summary>
    private void LeaderboardAPICheck()
    {
        for (int i = 0; i < 20; i++)
        {
            int number = i + 1;
            _leaderboardAPINames.Add(new LeaderboardPlayerAPINames($"{number}_Leaderboard_IGN", 
                $"{number}_Leaderboard_PreviousRoundPoints", 
                $"{number}_Leaderboard_OverallPoints"));
        }
    }
    private async Task UpdateLeaderboardTopAPI()
    {
        await UpdatePlayerHeads();

        for (int i = 0; i < 20; i++)
        {
            await _api.UpdateFileContent(_leaderboardAPINames[i].IGN, string.Empty);
            await _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, string.Empty);
            await _api.UpdateFileContent(_leaderboardAPINames[i].OverallPoints, string.Empty);
        }
        
        for (int i = 0; i < 20; i++)
        {
            if (i >= _leaderboardRepository.OrderedEntries.Count) break;
            
            var entry = _leaderboardRepository.OrderedEntries[i];
            var player = _playerRepository.GetPlayerByUUID(entry.PlayerUUID);
            if (player == null) continue;
            
            await _api.UpdateFileContent(_leaderboardAPINames[i].IGN, player.InGameName!);
            await _api.UpdateFileContent(_leaderboardAPINames[i].OverallPoints, entry.Points);

            if (_tournamentState.CurrentPreset.ManagementData is RankedManagementData data)
            {
                var previousRoundPoints = entry.Milestones.
                    OfType<EntryRankedMilestoneData>().
                    FirstOrDefault(e => e.Main.Milestone == _leaderboardRepository.Rules[0].ChosenAdvancement && e.Round == (data.Rounds - 1));
                
                await _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, previousRoundPoints?.Points ?? 0);
            }
            else
            {
                await _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, entry.Milestones[^1].Points);
            }
        }
    }
    private async Task UpdatePlayerHeads()
    {
        SceneItem[] itemsInHeadsGroup = [];
        try
        {
            itemsInHeadsGroup = await _obs.GetGroupSceneItemList("leaderboard_heads");
        }
        catch (Exception ex)
        {
            // Logger.Error(ex);
        }
        
        for (int i = 0; i < itemsInHeadsGroup.Length; i++)
        {
            var item = itemsInHeadsGroup[i];
            var sourceName = item.SourceName.ToLower();
            if (!sourceName.StartsWith("head")) continue;

            string numberLetters = sourceName[^2..];
            var digits = new string(numberLetters.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out int number)) continue;
            
            number -= 1;
            if (number is < 0 or > 19) continue;
            if (number >= _leaderboardRepository.OrderedEntries.Count)
            {
                _obs.SetBrowserURL(item.SourceName, string.Empty);
                continue;
            }
            var entry = _leaderboardRepository.OrderedEntries[number];

            string url = _controller.GetHeadURL(entry.PlayerUUID, 180);
            _obs.SetBrowserURL(item.SourceName, url);
        }
    }
}