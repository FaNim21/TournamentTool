using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.ViewModels;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.SidePanels;

public class RankedPlayer
{
    [JsonPropertyName("uuid")] 
    public string UUID { get; set; } = string.Empty;

    [JsonPropertyName("nickname")] 
    public string NickName { get; set; } = string.Empty;

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
public class RankedInventory
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
public class RankedTimeline
{
    [JsonPropertyName("uuid")] 
    public string UUID { get; set; } = string.Empty;

    [JsonPropertyName("type")] 
    public string Type { get; set; } = string.Empty; //maybe enum?? with all types ready

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("data")] 
    public int[] Data { get; set; } = [];

    [JsonPropertyName("shown")]
    public bool IsShown { get; set; }
}
public class RankedData
{
    [JsonPropertyName("matchType")]
    public string MatchType { get; set; } = string.Empty;

    [JsonPropertyName("category")] 
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("players")]
    public RankedPlayer[] Players { get; set; } = [];

    [JsonPropertyName("completes")] 
    public RankedComplete[] Completes { get; set; } = [];

    [JsonPropertyName("inventories")]
    public Dictionary<string, RankedInventory> Inventories { get; set; } = [];

    [JsonPropertyName("timelines")]
    public List<RankedTimeline> Timelines { get; set; } = [];
}

public class RankedPaceData
{
    public RankedPlayer Player { get; init; } = new();
    //eweutnalnie zrobic globaltimeline poniewaz obecny jest tylko dla rzeczywistych splitow
    public List<RankedTimeline> Timelines { get; init; } = [];
    public RankedInventory Inventory { get; set; } = new();
    public RankedComplete Completion { get; set; } = new();
    public int Resets { get; set; }
}
public class RankedBestSplit
{
    public string? PlayerName { get; set; }
    public RankedSplitType Type { get; set; }
    public long Time { get; set; }
}

public class RankedPacePanel : SidePanel, IRankedDataReceiver
{
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

    public int CompletedRunsCount { get; set; }

    public ICollectionView? GroupedRankedPaces { get; set; }


    public RankedPacePanel(ControllerViewModel controller) : base(controller)
    {
        Mode = ControllerMode.Ranked;
    }

    public override void OnEnable(object? parameter)
    {
        base.OnEnable(parameter);

        SetupRankedPaceGrouping();
    }
    public override bool OnDisable()
    {
        base.OnDisable();
        
        GroupedRankedPaces = null;
        return true;
    }

    private void SetupRankedPaceGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = Paces };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RankedPace.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPace.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(RankedPace.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedRankedPaces = collectionViewSource.View;
    }

    public void ReceivePaces(List<RankedPace> paces)
    {
        if (paces == null || paces.Count == 0)
        {
            Application.Current.Dispatcher.Invoke(() => { Paces.Clear(); });
            RefreshGroup();
            return;
        }
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            Paces.Clear();
            foreach (var pace in paces)
            {
                Paces.Add(pace);
            }
        });
        
        RefreshGroup();
    }

    public void UpdateAPIData(List<RankedBestSplit> bestSplits, int completedCount)
    {
        CompletedRunsCount = completedCount;
    }

    private void RefreshGroup()
    {
        if (GroupedRankedPaces == null) return;
        
        Application.Current.Dispatcher.Invoke(() => { GroupedRankedPaces.Refresh(); });
    }
}
