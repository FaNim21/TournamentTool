using TournamentTool.Core.Extensions;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging.Profiling;

namespace TournamentTool.Services.Background;

public record RankedPaceTimeline(string name, RunMilestone Milestone, long Time);
public record RankedPaceSplit(string Name, RankedSplitType Split, long Time);

public class RankedPace
{
    private RankedService _service;

    public Player Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    public string UUID { get; }
    public string InGameName { get; }
    public int EloRate { get; }
    public List<RankedPaceTimeline> Timelines { get; set; } = [];
    public List<RankedPaceSplit> Splits { get; set; } = [];
    public object? HeadImage { get; set; }
    public float HeadImageOpacity { get; set; }
    public int Resets { get; set; }
    public bool IsLive { get; set; }
    public RankedSplitType SplitType { get; set; }
    public string LastTimeline { get; set; } = string.Empty;
    public long CurrentSplitTimeMiliseconds { get; set; }
    public long DifferenceSplitTimeMiliseconds { get; set; }


    public RankedPace(RankedService service, PrivRoomPlayer privRoomPlayer, Player? player)
    {
        _service = service;
        
        UUID = privRoomPlayer.UUID;
        InGameName = privRoomPlayer.InGameName;
        EloRate = privRoomPlayer.EloRate ?? -1;
        Player = player ?? new Player();
    }
    public void Initialize()
    {
        //TODO: 5 TU JAK DODADZA INVENTORY DO API TO ZEBY DAC NA TRUE
        Inventory.DisplayItems = false;
        Splits.Add(new RankedPaceSplit("Start", RankedSplitType.Start, 0));
        
        UpdateHeadImage();
    }

    public void RestartedSeed()
    {
        Splits.Clear();
        Splits.Add(new RankedPaceSplit("Start", RankedSplitType.Start, 0));
        LastTimeline = string.Empty;

        UpdateLastTimelineAndSplit();
        Resets++;
    }

    public void AddTimeline(RankedPaceTimeline timeline)
    {
        if (Timelines.Count > 1 && timeline == Timelines[^1]) return;
        if (Timelines.Contains(timeline)) return;
        
        Timelines.Add(timeline);
        AddTimelineToEvaluation();
        
        UpdateSplit(timeline);
        UpdateLastTimelineAndSplit();
        
        if (timeline.Milestone != RunMilestone.ProjectEloReset) return;
        RestartedSeed();
    }

    public void Update(PrivRoomInventory inventory)
    {
        UpdateHeadImage();
        UpdateInventory(inventory);
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
        if (inventory == null)
        {
            Inventory.Clear();
            return;
        }
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
            string url = _service.SettingsService.Settings.HeadAPIType.GetHeadURL(UUID, 8);
            Task.Run(async () =>
            {
                HeadImage = await _service.ImageService.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            HeadImage = _service.ImageService.LoadImageFromStream(Player!.ImageStream ?? []);
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

    private void AddTimelineToEvaluation()
    {
        RankedPaceTimeline mainTimeline = Timelines[^1];
        RankedPaceTimeline? previousTimeline = null;
        if (Timelines.Count > 2)
        {
            previousTimeline = Timelines[^2];
        }

        if (Player == null) return;
        _service.AddEvaluationData(Player, mainTimeline, previousTimeline);
    }
    
    private void ValidateBestSplit(RankedPaceSplit pace)
    {
        PrivRoomBestSplit bestSplit = _service.GetBestSplit(pace.Split);

        for (int i = 0; i < bestSplit.Datas.Count; i++)
        {
            var current = bestSplit.Datas[i];
            if (current.PlayerName.Equals(InGameName)) return;
        }

        if (bestSplit.Datas.Count != 0)
        {
            PrivRoomBestSplitData? bestData = bestSplit.Datas[0];
            
            DifferenceSplitTimeMiliseconds = pace.Time - bestData.Time;
            if (DifferenceSplitTimeMiliseconds < 0) DifferenceSplitTimeMiliseconds = 0;
        }
        else
        {
            DifferenceSplitTimeMiliseconds = 0;
        }
        
        var data = new PrivRoomBestSplitData(InGameName, pace.Time, DifferenceSplitTimeMiliseconds);
        bestSplit.Datas.Add(data);
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