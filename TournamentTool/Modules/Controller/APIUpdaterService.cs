using OBSStudioClient.Classes;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Utils;
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

    private List<LeaderboardPlayerAPINames> _leaderboardAPINames = [];
    private const string _rankedPlayerCountFileName = "Ranked_players_count";
    
    
    public APIUpdaterService(ControllerViewModel controller, ILoggingService logger, TournamentViewModel preset, ObsController obs)
    {
        Logger = logger;
        _controller = controller;
        _preset = preset;
        _obs = obs;

        _api = new APIDataSaver();
        
        LeaderboardAPICheck();
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
        await UpdateLeaderboardTopAPI();
        // return Task.CompletedTask;
    }

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
    
    /// <summary>
    /// Rozwiazanie tymczasowe na potrzebe KOSTU polskiej ligi
    /// </summary>
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
            itemsInHeadsGroup = await _obs.GetGroupSceneItemList("heads");
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