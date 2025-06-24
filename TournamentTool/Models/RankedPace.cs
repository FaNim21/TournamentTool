using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public class RankedPace
{
    public class RankedTimelineSplit
    {
        public string Name { get; set; } = string.Empty;
        public RankedSplitType Split { get; set; }
        public long Time { get; set; }
    }
    
    private RankedService _service;
    
    public PlayerViewModel? Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    public string UUID { get; set; } = string.Empty;
    public string InGameName { get; init; } = string.Empty;
    public int EloRate { get; set; } = -1;
    public List<string> Timelines { get; set; } = [];
    public List<RankedTimelineSplit> Splits { get; set; } = [];
    public BitmapImage? HeadImage { get; set; }
    public int Resets { get; set; }
    public bool IsLive { get; set; }
    public float HeadImageOpacity { get; set; }
    public RankedSplitType SplitType { get; set; }
    public string LastTimeline { get; set; } = string.Empty;
    public long CurrentSplitTimeMiliseconds { get; set; }
    public long DifferenceSplitTimeMiliseconds { get; set; }

    private int lastCheckedTimelineIndex;


    public RankedPace(RankedService service)
    {
        _service = service;
    }
    
    public void Initialize(RankedPaceData data)
    {
        Inventory.DisplayItems = true;
        Splits.Add(new RankedTimelineSplit { Name = "Start", Split = RankedSplitType.Start, Time = 0 });
        
        Update(data);
    }

    public void Update(RankedPaceData data)
    {
        UpdateHeadImage();
        
        if (data.Timelines.Count == 0) return;
        if (Resets != data.Resets)
        {
            Timelines.Clear();
            Splits.Clear();
            Splits.Add(new RankedTimelineSplit { Name = "Start", Split = RankedSplitType.Start, Time = 0 });
            LastTimeline = string.Empty;

            //TODO: tu moze lepiej zgarnac timeline resetu i pobrac czas zeby na starcie nie bylo 00:00 tylko czas resetu z racji i tak rta czasu reszty splitow 
            UpdateLastSplit();
        }
        Resets = data.Resets;
        
        for (int i = lastCheckedTimelineIndex; i < data.Timelines.Count; i++)
        {
            var current = data.Timelines[i];
            UpdateSplit(current);
            Timelines.Add(current.Type);
        }
        lastCheckedTimelineIndex = data.Timelines.Count - 1;

        UpdateInventory(data.Inventory);
        UpdateLastSplit();

        if (Timelines.Count == 0) return;
        LastTimeline = Timelines[^1].CaptalizeAll();
    }

    private void UpdateSplit(RankedTimeline timeline)
    {
        bool wasFound = false;

        for (int j = 0; j < Splits.Count; j++)
        {
            var current = Splits[j];
            if (!current.Name.Equals(timeline.Type)) continue;
                
            wasFound = true;
            break;
        }
        if (wasFound) return;

        RankedTimelineSplit? newSplit = null;
        if (Enum.TryParse(typeof(RankedSplitType), timeline.Type, true, out var split))
        {
            newSplit = new RankedTimelineSplit { Name = timeline.Type, Split = (RankedSplitType)split, Time = timeline.Time };
        }
        else if ((timeline.Type.Equals("find_bastion") || timeline.Type.Equals("find_fortress")) && Splits.Count > 0)
        {
            var splitType = RankedSplitType.structure_2;

            if (Splits[^1].Name.Equals("enter_the_nether"))
                splitType = RankedSplitType.structure_1;

            newSplit = new RankedTimelineSplit { Name = timeline.Type, Split = splitType, Time = timeline.Time };
        }

        if (newSplit == null) return;
        
        ValidateBestSplit(newSplit);
        Splits.Add(newSplit);
    }
    
    private void UpdateInventory(RankedInventory inventory)
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
    private void UpdateLastSplit()
    {
        RankedTimelineSplit last = GetLastSplit();
        SplitType = last.Split;
        CurrentSplitTimeMiliseconds = last.Time;
    }

    private void ValidateBestSplit(RankedTimelineSplit newSplit)
    {
        RankedBestSplit bestSplit = _service.GetBestSplit(newSplit.Split);
        
        if(string.IsNullOrEmpty(bestSplit.PlayerName))
        {
            bestSplit.PlayerName = InGameName;
            bestSplit.Time = newSplit.Time;
        }
        DifferenceSplitTimeMiliseconds = newSplit.Time - bestSplit.Time;
        if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;
    }
    
    public RankedTimelineSplit GetLastSplit()
    {
        return GetSplit(1)!;
    }
    public RankedTimelineSplit? GetSplit(int indexFromEnd)
    {
        if (indexFromEnd > Splits.Count) return null;
        var split = Splits[^indexFromEnd];
        return new RankedTimelineSplit()
        {
            Name = split.Name,
            Split = split.Split,
            Time = split.Time
        };
    }
}