using System.Diagnostics;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.Utils.Parsers;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public record PacemanTimeline(string name, RunMilestone Milestone, long RTA, long IGT);

public class Paceman
{
    private PaceManService Service { get; }

    public string UUID { get; }
    public string Nickname { get; }
    public string WorldID { get; }
    public StreamDisplayInfo StreamDisplayInfo { get; }
    public bool ShowOnlyLive { get; private set; }

    public PlayerViewModel? PlayerViewModel { get; set; }
    public PlayerInventory Inventory { get; set; }
    
    public List<PacemanTimeline> Splits { get; set; } = [];
    public List<PacemanTimeline> Advancements { get; set; } = [];
    
    public PacemanTimeline? LastTimeline { get; private set; }
    public PacemanTimeline? LastAdvancement { get; private set; }

    public BitmapImage? HeadImage { get; set; }
    public float HeadImageOpacity { get; set; }
    public bool IsLive { get; set; }
    public SplitType SplitType { get; set; }

    public long CurrentSplitTimeMiliseconds { get; set; }
    public long IGTTimeMiliseconds { get; set; }
    
    public bool IsPacePrioritized { get; set; }

    private long _lastUpdate;
    private int _lastTimelineIndex = 0;
    private int _lastAdvancementIndex = 0;

    
    public Paceman(PaceManService service, PaceManData data, PlayerViewModel playerViewModel)
    {
        Service = service;
        PlayerViewModel = playerViewModel;
        
        Nickname = data.Nickname;
        WorldID = data.WorldID;
        UUID = data.User.UUID;
        StreamDisplayInfo = new StreamDisplayInfo(data.User.TwitchName, StreamType.twitch);

        Inventory = new PlayerInventory();
        if (!data.ShowOnlyLive) IsLive = data.IsLive();
        
        UpdateHeadImage();
        Update(data);
    }
    
    public void Update(PaceManData paceman)
    {
        ShowOnlyLive = paceman.ShowOnlyLive;
        _lastUpdate = paceman.LastUpdate;
        if (paceman.Splits.Count == 0) return;
        
        for (; _lastTimelineIndex < paceman.Splits.Count; _lastTimelineIndex++)
        {
            var current = paceman.Splits[_lastTimelineIndex];
            var milestone = RunMilestoneParser.Parse(current.SplitName);
            var timeline = new PacemanTimeline(milestone.Name, milestone.Milestone, current.RTA, current.IGT);
            
            Splits.Add(timeline);
            UpdateSplit(timeline);

            PacemanTimeline? previous = null;
            if (Splits.Count > 1)
            {
                previous = Splits[^2];
            }
            AddTimelineToEvaluation(timeline, previous);
        }
        
        for (; _lastAdvancementIndex < paceman.Advancements.Count; _lastAdvancementIndex++)
        {
            var current = paceman.Advancements[_lastAdvancementIndex];
            var milestone = RunMilestoneParser.Parse(current.SplitName);
            var timeline = new PacemanTimeline(milestone.Name, milestone.Milestone, current.RTA, current.IGT);
            
            Advancements.Add(timeline);
            LastAdvancement = timeline;
            
            PacemanTimeline? previous = null;
            if (Advancements.Count > 1)
            {
                previous = Advancements[^2];
            }
            AddTimelineToEvaluation(timeline, previous);
        }
        
        UpdateInventory(paceman.ItemsData);
        UpdateIGTTime();
    }
    
    private void UpdateHeadImage()
    {
        if (HeadImage != null) return;

        if (PlayerViewModel == null)
        {
            string url = $"https://minotar.net/helm/{UUID}/8.png";
            HeadImageOpacity = 0.35f;
            Task.Run(async () =>
            {
                HeadImage = await Helper.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            HeadImageOpacity = 1f;
            PlayerViewModel.LoadHead();
            HeadImage = PlayerViewModel.Image;
        }
    }

    private void UpdateInventory(PaceItemData items)
    {
        if (items.EstimatedCounts == null) return;
        
        items.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
        items.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
        if (Splits.Count > 2 && !Inventory.DisplayItems) Inventory.DisplayItems = true;

        Inventory.BlazeRodsCount = estimatedRods;
        Inventory.PearlsCount = estimatedPearls;
    }
    private void UpdateSplit(PacemanTimeline timeline)
    {
        if (Splits.Count > 1 && timeline.Milestone is RunMilestone.PacemanEnterNether) return;
        
        if (timeline.Milestone is RunMilestone.PacemanEnterBastion or RunMilestone.PacemanEnterFortress)
        {
            if (Splits.Count > 2 && Splits[^2].Milestone == RunMilestone.PacemanEnterNether)
            {
                SplitType = SplitType.structure_2;
            }
            else if (Splits.Count > 1)
            {
                SplitType = Splits[^2].Milestone == RunMilestone.PacemanEnterNether ? SplitType.structure_1 : SplitType.structure_2;
            }
            else
            {
                SplitType = SplitType.structure_1;
            }
        }
        else
        {
            SplitType = Enum.Parse<SplitType>(timeline.name);
        }
        
        SetPacePriority(Service.CheckForGoodPace(SplitType, timeline));
        CurrentSplitTimeMiliseconds = timeline.IGT;
        LastTimeline = timeline;
    }
    private void UpdateIGTTime()
    {
        var lastTimelineIGT = LastTimeline?.IGT ?? 0;
        IGTTimeMiliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - _lastUpdate + lastTimelineIGT;
    }

    private void AddTimelineToEvaluation(PacemanTimeline main, PacemanTimeline? previous)
    {
        LeaderboardTimeline mainTimeline = new LeaderboardTimeline(main.Milestone, (int)main.IGT);
        LeaderboardTimeline? previousTimeline = null;
        if (previous != null)
        {
            previousTimeline = new LeaderboardTimeline(previous.Milestone, (int)previous.IGT);
        }

        if (PlayerViewModel == null) return;
        Service.AddEvaluationData(PlayerViewModel.Data, WorldID, mainTimeline, previousTimeline);
    }
    
    private void SetPacePriority(bool good)
    {
        IsPacePrioritized = good;
    }
}