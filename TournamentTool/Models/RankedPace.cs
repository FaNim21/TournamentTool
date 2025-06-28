using System.Windows.Media.Imaging;
using MethodTimer;
using TournamentTool.Enums;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public record RankedPaceTimeline(string name, RunMilestone Milestone, long Time);
public record RankedPaceSplit(string Name, RankedSplitType Split, long Time);

public class RankedPace
{
    private RankedService _service;
    
    public PlayerViewModel? Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    public string UUID { get; set; } = string.Empty;
    public string InGameName { get; init; } = string.Empty;
    public int EloRate { get; set; } = -1;
    public List<RankedPaceTimeline> Timelines { get; set; } = [];
    public List<RankedPaceSplit> Splits { get; set; } = [];
    public BitmapImage? HeadImage { get; set; }
    public float HeadImageOpacity { get; set; }
    public int Resets { get; set; }
    public bool IsLive { get; set; }
    public RankedSplitType SplitType { get; set; }
    public string LastTimeline { get; set; } = string.Empty;
    public long CurrentSplitTimeMiliseconds { get; set; }
    public long DifferenceSplitTimeMiliseconds { get; set; }

    private int lastCheckedTimelineIndex;


    public RankedPace(RankedService service)
    {
        _service = service;
    }
    public void Initialize(PrivRoomPaceData data)
    {
        Inventory.DisplayItems = true;
        Splits.Add(new RankedPaceSplit("Start", RankedSplitType.Start, 0));
        
        Update(data);
    }

    public void Update(PrivRoomPaceData data)
    {
        UpdateHeadImage();
        
        if (Resets != data.Resets)
        {
            Timelines.Clear();
            Splits.Clear();
            Splits.Add(new RankedPaceSplit("Start", RankedSplitType.Start, 0));
            LastTimeline = string.Empty;

            UpdateLastTimelineAndSplit();
        }
        Resets = data.Resets;
        
        for (int i = lastCheckedTimelineIndex; i < data.Timelines.Count; i++)
        {
            var current = data.Timelines[i];
            
            Timelines.Add(current);
            UpdateSplit(current);
        }
        
        lastCheckedTimelineIndex = Math.Max(data.Timelines.Count, 0);

        UpdateInventory(data.Inventory);
        UpdateLastTimelineAndSplit();
    }

    private void UpdateSplit(RankedPaceTimeline timeline)
    {
        bool wasFound = false;
        string name = timeline.name;

        for (int j = 0; j < Splits.Count; j++)
        {
            var current = Splits[j];
            if (!current.Name.Equals(name)) continue;
                
            wasFound = true;
            break;
        }
        if (wasFound) return;

        RankedPaceSplit? newSplit = null;
        if (Enum.TryParse(typeof(RankedSplitType), name, true, out var split))
        {
            newSplit = new RankedPaceSplit(name, (RankedSplitType)split, timeline.Time);
        }
        else if ((name.Equals("find_bastion") || name.Equals("find_fortress")) && Splits.Count > 0)
        {
            var splitType = RankedSplitType.structure_2;

            if (Splits[^1].Name.Equals("enter_the_nether"))
                splitType = RankedSplitType.structure_1;

            newSplit = new RankedPaceSplit(name, splitType, timeline.Time); 
        }

        if (newSplit == null) return;
        
        ValidateBestSplit(newSplit);
        Splits.Add(newSplit);
    }
    
    private void UpdateInventory(PrivRoomInventory inventory)
    {
        Inventory.BlazeRodsCount = inventory.BlazeRod;
        Inventory.ObsidianCount = inventory.Obsidian;
        Inventory.BedsCount = inventory.WhiteBed;
        Inventory.EnderEyeCount = inventory.EnderEye;
        Inventory.PearlsCount = inventory.EnderPearl;
    }
    private void UpdateHeadImage()
    {
        IsLive = Player != null && !Player.StreamData.AreBothNullOrEmpty();
        HeadImageOpacity = HeadImage != null && IsLive ? 1f : .25f;
        if (HeadImage != null) return;

        if (Player == null)
        {
            string url = $"https://minotar.net/helm/{InGameName}/8.png";
            Task.Run(async () =>
            {
                HeadImage = await Helper.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            HeadImage = Player!.Image;
        }
    }
    private void UpdateLastTimelineAndSplit()
    {
        RankedPaceSplit last = GetLastSplit();
        SplitType = last.Split;
        CurrentSplitTimeMiliseconds = last.Time;
        
        if (Timelines.Count == 0) return;
        LastTimeline = Timelines[^1].name.CaptalizeAll();
    }

    private void ValidateBestSplit(RankedPaceSplit newSplit)
    {
        PrivRoomBestSplit bestSplit = _service.GetBestSplit(newSplit.Split);
        
        if(string.IsNullOrEmpty(bestSplit.PlayerName))
        {
            bestSplit.PlayerName = InGameName;
            bestSplit.Time = newSplit.Time;
        }
        DifferenceSplitTimeMiliseconds = newSplit.Time - bestSplit.Time;
        if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;
    }
    
    public RankedPaceSplit GetLastSplit()
    {
        return GetSplit(1)!;
    }
    public RankedPaceSplit? GetSplit(int indexFromEnd)
    {
        if (indexFromEnd > Splits.Count) return null;
        var split = Splits[^indexFromEnd];
        return new RankedPaceSplit(split.Name, split.Split, split.Time);
    }
}