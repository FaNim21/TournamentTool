using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Modules.SidePanels;

public struct RankedPlayer
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }

    [JsonPropertyName("nickname")]
    public string NickName { get; set; }

    [JsonPropertyName("roleType")]
    public byte RoleType { get; set; }

    [JsonPropertyName("eloRate")]
    public int? EloRate { get; set; }

    [JsonPropertyName("eloRank")]
    public int? EloRank { get; set; }
}
public struct RankedComplete
{
    [JsonPropertyName("player")]
    public string UUID { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }
}
public struct RankedInventory
{
    [JsonPropertyName("splash_potion")]
    public int? SplashPotions { get; set; }

    [JsonPropertyName("gold_block")]
    public int GoldBlocks { get; set; }

    [JsonPropertyName("iron_ingot")]
    public int IronIngots { get; set; }

    [JsonPropertyName("obsidian")]
    public int Obsidian { get; set; }

    [JsonPropertyName("glowstone_dust")]
    public int GlowstoneDust { get; set; }

    [JsonPropertyName("string")]
    public int String { get; set; }

    [JsonPropertyName("crying_obsidian")]
    public int CryingObsidian { get; set; }

    [JsonPropertyName("ender_pearl")]
    public int EnderPearl { get; set; }

    [JsonPropertyName("iron_nugget")]
    public int IronNugger { get; set; }

    [JsonPropertyName("diamond")]
    public int Diamond { get; set; }

    [JsonPropertyName("white_bed")]
    public int WhiteBed { get; set; }

    [JsonPropertyName("glowstone")]
    public int GlowStone { get; set; }

    [JsonPropertyName("ender_eye")]
    public int EnderEye { get; set; }

    [JsonPropertyName("blaze_rod")]
    public int BlazeRod { get; set; }

    [JsonPropertyName("gold_ingot")]
    public int GoldIngot { get; set; }

    [JsonPropertyName("white_wool")]
    public int WhiteWool { get; set; }

    [JsonPropertyName("blaze_powder")]
    public int BlazePowder { get; set; }

    [JsonPropertyName("potion")]
    public int Potion { get; set; }
}
public struct RankedTimeline
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } //maybe enum?? with all types ready

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("data")]
    public int[] Data { get; set; }

    [JsonPropertyName("shown")]
    public bool IsShown { get; set; }
}
public readonly struct RankedData
{
    [JsonPropertyName("matchType")]
    public string MatchType { get; init; }

    [JsonPropertyName("category")]
    public string Category { get; init; }

    [JsonPropertyName("startTime")]
    public long StartTime { get; init; }

    [JsonPropertyName("players")]
    public RankedPlayer[] Players { get; init; }

    [JsonPropertyName("completes")]
    public RankedComplete[] Completes { get; init; }

    [JsonPropertyName("inventories")]
    public Dictionary<string, RankedInventory> Inventories { get; init; }

    [JsonPropertyName("timelines")]
    public List<RankedTimeline> Timelines { get; init; }
}

public struct RankedPaceData
{
    public RankedPlayer Player { get; set; }
    //eweutnalnie zrobic globaltimeline poniewaz obecny jest tylko dla rzeczywistych splitow
    public List<RankedTimeline> Timelines { get; set; }
    public RankedInventory Inventory { get; set; }
    public RankedComplete Completion { get; set; }
    public int Resets { get; set; }
}

public class RankedBestSplit()
{
    public string? PlayerName { get; set; }
    public RankedSplitType Type { get; set; }
    public long Time { get; set; }
}

public class RankedPacePanel : SidePanel
{
    private readonly APIDataSaver API;
    private readonly JsonSerializerOptions _options;

    private CancellationTokenSource? _cancellationTokenSource;

    private string FilePath { get; set; } = string.Empty;

    private ObservableCollection<RankedPace> _paces = [];
    public ObservableCollection<RankedPace> Paces
    {
        get => _paces;
        set
        {
            _paces = value;
            OnPropertyChanged(nameof(Paces));
        }
    }

    private ObservableCollection<RankedBestSplit> _bestSplits = [];
    public ObservableCollection<RankedBestSplit> BestSplits
    {
        get => _bestSplits;
        set
        {
            _bestSplits = value;
            OnPropertyChanged(nameof(BestSplits));
        }
    }

    public long StartTime { get; set; }
    public int CompletedRunsCount { get; set; }
    public int PacesNotFromWhitelistAmount { get; set; }

    public ICollectionView? GroupedRankedPaces { get; set; }

    private string _rankedPlayerCountFileName = "Ranked_players_count";
    private string _rankedCompletedCountFileName = "Ranked_completes_count";


    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        _options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            PropertyNameCaseInsensitive = true
        };

        API = new APIDataSaver();
        API.CheckFile(_rankedPlayerCountFileName);
        API.CheckFile(_rankedCompletedCountFileName);
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
        SetupPaceManGrouping();

        string dataName = Controller.Configuration.RankedRoomDataName;
        if(!dataName.EndsWith(".json"))
        {
            dataName = Path.Combine(dataName, ".json");
        }
        FilePath = Path.Combine(Controller.Configuration.RankedRoomDataPath, dataName);
        _cancellationTokenSource = new();

        if (!File.Exists(FilePath))
        {
            DialogBox.Show($"There is not file on: {FilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Task.Run(UpdateSpectatorMatch);
    }
    public override bool OnDisable()
    {
        base.OnDisable();

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        return true;
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = Paces };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RankedPace.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPace.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPace.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedRankedPaces = collectionViewSource.View;
    }

    private async Task UpdateSpectatorMatch()
    {
        var cancellationToken = _cancellationTokenSource!.Token;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LoadJsonFileAsync();
                UpdateAPI();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(Controller.Configuration.RankedRoomUpdateFrequency), cancellationToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    private void UpdateAPI()
    {
        API.UpdateFileContent(_rankedPlayerCountFileName, Paces.Count);
        API.UpdateFileContent(_rankedCompletedCountFileName, CompletedRunsCount); //TODO: 0 count completions
    }

    private async Task LoadJsonFileAsync()
    {
        RankedData? rankedData = null;
        try
        {
            string jsonContent;

            using (FileStream fs = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

        if (!rankedData.HasValue) return;

        FilterJSON(rankedData.Value);

        CompletedRunsCount = rankedData.Value.Completes.Length;
        Application.Current.Dispatcher.Invoke(() => { GroupedRankedPaces?.Refresh(); });
    }
    private void FilterJSON(RankedData rankedData)
    {
        if (rankedData.Timelines.Count == 0)
        {
            Application.Current.Dispatcher.Invoke(Paces.Clear);
        }

        //Seed change | New match | 
        if(rankedData.StartTime != StartTime)
        {
            StartTime = rankedData.StartTime;
            Application.Current.Dispatcher.Invoke(() =>
            {
                BestSplits.Clear();
                Paces.Clear();
            });
        }

        PacesNotFromWhitelistAmount = 0;
        List<RankedPace> _paces = new(Paces);
        for (int i = 0; i < rankedData.Players.Length; i++)
        {
            var player = rankedData.Players[i];
            RankedPaceData data = new()
            {
                Player = player,
                Timelines = []
            };

            rankedData.Inventories.TryGetValue(player.UUID, out var inventory);
            data.Inventory = inventory;
            if (data.Inventory.SplashPotions == null) continue;

            for (int j = 0; j < rankedData.Completes.Length; j++)
            {
                var completion = rankedData.Completes[j];
                if (completion.UUID.Equals(player.UUID))
                {
                    data.Completion = completion;
                    break;
                }
            }

            for (int j = 0; j < rankedData.Timelines.Count; j++)
            {
                var timeline = rankedData.Timelines[j];
                if (timeline.Type.EndsWith("root")) continue;

                if (timeline.UUID.Equals(player.UUID))
                {
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
            }

            bool wasFoundOnPaces = false;
            for (int j = 0; j < _paces.Count; j++)
            {
                var pace = _paces[j];
                if (pace.InGameName.Equals(data.Player.NickName))
                {
                    Application.Current.Dispatcher.Invoke(() => { pace.Update(data); });
                    wasFoundOnPaces = true;
                    _paces.Remove(pace);
                    break;
                }
            }

            if (!wasFoundOnPaces) AddPace(data);
        }

        for (int i = 0; i < _paces.Count; i++)
        {
            RemovePace(_paces[i]);
        }
    }

    private void AddPace(RankedPaceData data)
    {
        RankedPace pace = new(Controller, this);
        pace.Initialize(data.Player);

        int n = Controller.Configuration.Players.Count;
        bool found = false;
        for (int j = 0; j < n; j++)
        {
            var current = Controller.Configuration.Players[j];

            if (current.InGameName!.Equals(pace.InGameName, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                pace.Player = current;
                break;
            }
        }
        if (!found)
        {
            PacesNotFromWhitelistAmount += 1;
            return;
        }

        pace.Inventory.DisplayItems = true;
        pace.Update(data);

        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Add(pace);
        });
    }
    private void RemovePace(RankedPace pace)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Remove(pace);
        });
    }

    public RankedBestSplit GetBestSplit(RankedSplitType splitType)
    {
        for (int i = 0; i < BestSplits.Count; i++)
        {
            var split = BestSplits[i];
            if (split.Type.Equals(splitType)) return split;
        }

        RankedBestSplit bestSplit = new() { Type = splitType };
        BestSplits.Add(bestSplit);
        return bestSplit;
    }
}
