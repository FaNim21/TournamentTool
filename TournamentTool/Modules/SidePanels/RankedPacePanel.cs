using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
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
    public int SplashPotions { get; set; }

    [JsonPropertyName("gold_block")]
    public int GoldBlocks { get; set; }

    [JsonPropertyName("iron_ingot")]
    public int IronIngots { get; set; }

    [JsonPropertyName("obsidian")]
    public int Obsidian { get; set; }

    [JsonPropertyName("glowstone_dust")]
    public int GlowstoneDust { get; set; }

    //TODO: 0 and rest to add
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

public struct RankedData
{
    [JsonPropertyName("matchType")]
    public string MatchType { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("players")]
    public RankedPlayer[] Players { get; set; }

    [JsonPropertyName("completes")]
    public RankedComplete[] Completes { get; set; }

    [JsonPropertyName("inventories")]
    public Dictionary<string, RankedInventory> Inventories { get; set; }

    [JsonPropertyName("timelines")]
    public RankedTimeline[] Timelines { get; set; }
}

public class RankedPacePanel : SidePanel
{
    private readonly FileSystemWatcher _fileWatcher;
    private readonly JsonSerializerOptions _options;

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

 
    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        _fileWatcher = new FileSystemWatcher();

        _options = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString,
            PropertyNameCaseInsensitive = true
        };
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);
        string dataName = Controller.Configuration.RankedRoomDataName;
        if(!dataName.EndsWith(".json"))
        {
            dataName = Path.Combine(dataName, ".json");
        }
        FilePath = Path.Combine(Controller.Configuration.RankedRoomDataPath, dataName);

        _fileWatcher.Path = Controller.Configuration.RankedRoomDataPath;
        _fileWatcher.Filter = dataName;
        _fileWatcher.NotifyFilter = NotifyFilters.LastWrite;

        _fileWatcher.Changed += OnJsonFileChanged;
        _fileWatcher.EnableRaisingEvents = true;

        //TEMP do tego zeby nie edytowac pliku do szybkiego podgladu danych
        LoadJsonFileAsync();
    }

    public override bool OnDisable()
    {
        base.OnDisable();
        _fileWatcher.Changed -= OnJsonFileChanged;
        _fileWatcher.EnableRaisingEvents = false;

        return true;
    }

    private async void OnJsonFileChanged(object sender, FileSystemEventArgs e)
    {
        await LoadJsonFileAsync();
    }

    private async Task LoadJsonFileAsync()
    {
        try
        {
            string jsonContent;

            using (FileStream fs = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new(fs))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            RankedData? data = JsonSerializer.Deserialize<RankedData>(jsonContent, _options);

            if (!data.HasValue) return;

            Application.Current.Dispatcher.Invoke(Paces.Clear);
            for (int i = 0; i < data.Value.Players.Length; i++)
            {
                var player = data.Value.Players[i];
                AddPlayer(player);
            }
        }
        catch (Exception ex)
        {
            DialogBox.Show("Error reading JSON file: " + ex.Message, "", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void AddPlayer(RankedPlayer player)
    {
        RankedPace playerData = new()
        {
            InGameName = player.NickName,
            EloRate = player.EloRate ?? -1,   
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Add(playerData);
        });
    }
}
