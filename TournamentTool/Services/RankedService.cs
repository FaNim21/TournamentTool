using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;
using MethodTimer;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.SidePanels;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Services;

public class RankedService : IBackgroundService
{
    private TournamentViewModel TournamentViewModel { get; }
    private LeaderboardPanelViewModel Leaderboard { get; }

    private IRankedDataReceiver? _rankedDataReceiver;

    private long _startTime;
    private int _pacesNotFromWhitelistAmount;
    private int _completedRunsCount;
    
    private List<RankedPace> _paces = [];
    private List<RankedBestSplit> _bestSplits = [];
    
    private readonly JsonSerializerOptions _options;
    private readonly string _filePath;
    
    
    public RankedService(TournamentViewModel tournamentViewModel, LeaderboardPanelViewModel leaderboard)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        
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
        
        if (!File.Exists(_filePath))
        {
            DialogBox.Show($"There is not file on: {_filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public void RegisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver is IRankedDataReceiver ranked)
        {
            _rankedDataReceiver = ranked;
        }
    }
    public void UnregisterData(IBackgroundDataReceiver? receiver)
    {
        if (receiver == _rankedDataReceiver) _rankedDataReceiver = null;
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
            string jsonContent;

            await using (FileStream fs = new(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new(fs))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            rankedData = JsonSerializer.Deserialize<RankedData>(jsonContent, _options);
        }
        catch (Exception ex)
        {
            Trace.WriteLine("Error reading JSON file: " + ex.Message);
        }

        if (rankedData == null) return;

        FilterJSON(rankedData);
        _rankedDataReceiver?.ReceivePaces(_paces);
        
        _completedRunsCount = rankedData.Completes.Length;
        _rankedDataReceiver?.UpdateAPIData(_bestSplits, _completedRunsCount);
    }
    
    [Time]
    private void FilterJSON(RankedData rankedData)
    {
        if (rankedData.Timelines.Count == 0)
        {
            _paces.Clear();
        }

        //Seed change | New match |
        if(rankedData.StartTime != _startTime)
        {
            _startTime = rankedData.StartTime;
            _bestSplits.Clear();
            _paces.Clear();
        }

        _pacesNotFromWhitelistAmount = 0;
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
        RankedPace pace = new(this);
        pace.Initialize(data.Player);

        int n = TournamentViewModel.Players.Count;
        bool found = false;
        for (int j = 0; j < n; j++)
        {
            var current = TournamentViewModel.Players[j];
            if (!current.InGameName!.Equals(pace.InGameName, StringComparison.OrdinalIgnoreCase)) continue;
            
            found = true;
            pace.Player = current;
            break;
        }
        if (!found)
        {
            _pacesNotFromWhitelistAmount += 1;
            return;
        }

        pace.Inventory.DisplayItems = true;
        pace.Update(data);

        _paces.Add(pace);
    }
    private void RemovePace(RankedPace pace)
    {
        _paces.Remove(pace);
    }
    
    public RankedBestSplit GetBestSplit(RankedSplitType splitType)
    {
        for (int i = 0; i < _bestSplits.Count; i++)
        {
            var split = _bestSplits[i];
            if (split.Type.Equals(splitType)) return split;
        }

        RankedBestSplit bestSplit = new() { Type = splitType };
        _bestSplits.Add(bestSplit);
        return bestSplit;
    }
}