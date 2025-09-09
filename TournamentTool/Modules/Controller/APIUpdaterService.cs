using OBSStudioClient.Classes;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.Modules.Controller;

public class APIUpdaterService : IServiceUpdater
{
    private ILoggingService Logger { get; }
    
    private readonly ControllerViewModel _controller;
    private readonly TournamentViewModel _preset;
    private readonly ObsController _obs;
    private readonly APIDataSaver _api;

    private record LeaderboardPlayerAPINames(string IGN, string PreviousRoundPoints, string OverallPoints);
    private record SplitsTwoPlayersAPINames(string Time, string TimeDifference);

    private List<LeaderboardPlayerAPINames> _leaderboardAPINames = [];
    private List<SplitsTwoPlayersAPINames>[] _splitsAPINames = [];
    
    private string[] _rankedSplits = ["enter_the_nether", "structure_1", "structure_2", 
        "blind_travel", "follow_ender_eye", "enter_the_end", "kill_dragon", "complete"];

    private string[] _rankedNames = ["1_split_player_name", "2_split_player_name"];
    private string[] _savedNames = new string[2];
    
    
    public APIUpdaterService(ControllerViewModel controller, ILoggingService logger, TournamentViewModel preset, ObsController obs)
    {
        Logger = logger;
        _controller = controller;
        _preset = preset;
        _obs = obs;

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
        _controller.ManagementPanel?.UpdateAPI(_api);
        try
        {
            await UpdateLeaderboardTopAPI();
            await UpdateTwoPlayersSplits();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        // return Task.CompletedTask;
    }

    /// <summary>
    /// HARD CODED Rozwiazanie tymczasowe na potrzebe KOSTU polskiej ligi
    /// </summary>
    private void UpdateTwoPlayersAPICheck()
    {
        _api.CheckFile(_rankedNames[0]);
        _api.CheckFile(_rankedNames[1]);

        _splitsAPINames = new List<SplitsTwoPlayersAPINames>[2];
        _splitsAPINames[0] = [];
        _splitsAPINames[1] = [];
        
        for (int i = 0; i < 2; i++)
        {
            int number = i + 1;
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                string currentSplit = _rankedSplits[j];
                _splitsAPINames[i].Add(new SplitsTwoPlayersAPINames($"{number}_{currentSplit}_Time", 
                    $"{number}_{currentSplit}_Difference"));
            
                _api.CheckFile(_splitsAPINames[i][j].Time);
                _api.CheckFile(_splitsAPINames[i][j].TimeDifference);
            }
        }
    }
    private async Task UpdateTwoPlayersSplits()
    {
        if (_preset.ManagementData is not RankedManagementData rankedManagementData) return;
        
        SceneItem[] itemsInHeadsGroup = [];
        try
        {
            itemsInHeadsGroup = await _obs.GetGroupSceneItemList("splits_heads");
        }
        catch { /**/ }

        int count = 14;
        for (int i = 0; i < itemsInHeadsGroup.Length; i++)
        {
            var item = itemsInHeadsGroup[i];
            var sourceName = item.SourceName.ToLower();
            if (!sourceName.StartsWith("split_head")) continue;

            string numberLetters = sourceName[^2..];
            var digits = new string(numberLetters.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, out int number)) continue;
            
            number -= 1;
            if (number is < 0 or > 1) continue;
            if (rankedManagementData.BestSplits.Count == 0) return;
            if (i >= rankedManagementData.BestSplits[0].Datas.Count) return;
            if (number >= rankedManagementData.BestSplits[0].Datas.Count)
            {
                _obs.SetBrowserURL(item.SourceName, string.Empty);
                continue;
            }

            count++;
            var data = rankedManagementData.BestSplits[0].Datas[count];

            string path = $"https://mc-heads.net/avatar/{data.PlayerName}/180";
            _obs.SetBrowserURL(item.SourceName, path);
            _savedNames[i] = data.PlayerName;
            _api.UpdateFileContent(_rankedNames[i], _savedNames[i]);
        }
        
        UpdateTwoPlayersSplitsData(rankedManagementData);
    }
    private void UpdateTwoPlayersSplitsData(RankedManagementData rankedManagementData)
    {
        List<long> player1Times = []; 
        List<long> player2Times = []; 
        
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                PrivRoomBestSplit? currentSplit = null;
                for (int k = 0; k < rankedManagementData.BestSplits.Count; k++)
                {
                    var best = rankedManagementData.BestSplits[k];
                    if (!best.Type.ToString().Equals(_rankedSplits[j])) continue;
                    
                    currentSplit = rankedManagementData.BestSplits[k];
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
                    if (i == 0)
                        player1Times.Add(data.Time);
                    else
                        player2Times.Add(data.Time);
                    
                    foundData = true;
                    break;
                }

                if (foundData) continue;
                _api.UpdateFileContent(_splitsAPINames[i][j].Time, string.Empty);
                _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
            }
        }
        
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < _rankedSplits.Length; j++)
            {
                if (j >= player1Times.Count || j >= player2Times.Count)
                {
                    _api.UpdateFileContent(_splitsAPINames[i][j].TimeDifference, string.Empty);
                    continue;
                }

                long difference = i == 0 ? player2Times[j] - player1Times[j] : player1Times[j] - player2Times[j];
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
            
            _api.CheckFile(_leaderboardAPINames[i].IGN);
            _api.CheckFile(_leaderboardAPINames[i].PreviousRoundPoints);
            _api.CheckFile(_leaderboardAPINames[i].OverallPoints);
        }
    }
    private async Task UpdateLeaderboardTopAPI()
    {
        await UpdatePlayerHeads();
        
        for (int i = 0; i < 20; i++)
        {
            if (i >= _preset.Leaderboard.OrderedEntries.Count)
            {
                _api.UpdateFileContent(_leaderboardAPINames[i].IGN, string.Empty);
                _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, string.Empty);
                _api.UpdateFileContent(_leaderboardAPINames[i].OverallPoints, string.Empty);
                continue;
            }
            
            var entry = _preset.Leaderboard.OrderedEntries[i];
            var player = _preset.GetPlayerByUUID(entry.PlayerUUID);
            if (player == null) continue;
            
            _api.UpdateFileContent(_leaderboardAPINames[i].IGN, player.InGameName!);
            _api.UpdateFileContent(_leaderboardAPINames[i].OverallPoints, entry.Points);

            if (_preset.ManagementData is RankedManagementData data)
            {
                var previousRoundPoints = entry.Milestones.
                    OfType<EntryRankedMilestoneData>().
                    FirstOrDefault(e => e.Main.Milestone == _preset.Leaderboard.Rules[0].ChosenAdvancement && e.Round == data.Rounds);
                
                _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, previousRoundPoints?.Points ?? 0);
            }
            else
            {
                _api.UpdateFileContent(_leaderboardAPINames[i].PreviousRoundPoints, entry.Milestones[^1].Points);
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
        catch { /**/ }
        
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
            if (number >= _preset.Leaderboard.OrderedEntries.Count)
            {
                _obs.SetBrowserURL(item.SourceName, string.Empty);
                continue;
            }
            var entry = _preset.Leaderboard.OrderedEntries[number];

            string path = $"https://mc-heads.net/avatar/{entry.PlayerUUID}/180";
            _obs.SetBrowserURL(item.SourceName, path);
        }
    }
}