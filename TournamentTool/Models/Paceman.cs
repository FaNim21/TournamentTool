using System.Diagnostics;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

public class Paceman
{
    public PaceManData Data { get; private set; }

    private PaceManService Service { get; }

    public string Nickname => Data.Nickname;
    public string WorldID => Data.WorldID;

    public PlayerViewModel? PlayerViewModel { get; set; }
    public PlayerInventory Inventory { get; set; }

    public BitmapImage? HeadImage { get; set; }
    public float HeadImageOpacity { get; set; }
    public bool IsLive { get; set; }
    public SplitType SplitType { get; set; }

    public long CurrentSplitTimeMiliseconds { get; set; }
    public long IGTTimeMiliseconds { get; set; }
    
    public bool IsPacePrioritized { get; set; }

    private PacemanPaceMilestone? LastMilestone { get; set; } = null;

    
    public Paceman(PaceManService service, PaceManData data, PlayerViewModel playerViewModel)
    {
        Service = service;
        Data = data;
        PlayerViewModel = playerViewModel;

        Inventory = new PlayerInventory();
        if (!Data.ShowOnlyLive) IsLive = Data.IsLive();
        
        UpdateHeadImage();
        UpdateTime();
    }
    
    public void Update(PaceManData paceman)
    {
        Data = paceman;
        
        if (Data.Splits.Count == 0) return;
        UpdateTime();
    }
    private void UpdateTime()
    {
        PacemanPaceMilestone lastMilestone = GetLastSplit();
        lastMilestone.SplitName = lastMilestone.SplitName.Replace("rsg.", "");

        if (LastMilestone == null || !LastMilestone!.SplitName.Equals(lastMilestone.SplitName))
        {
            string milestone = LastMilestone == null ? "None" : $"{LastMilestone.SplitName}";
            Trace.WriteLine($"{Nickname} player from {milestone} to {lastMilestone!.SplitName}");
            UpdateLastSplit(lastMilestone);
            Service.EvaluatePlayerInLeaderboard(this);
        }
        
        UpdateIGTTime();
    }
    private void UpdateHeadImage()
    {
        if (HeadImage != null) return;

        if (PlayerViewModel == null)
        {
            string url = $"https://minotar.net/helm/{Data.User.UUID}/8.png";
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

    private void UpdateLastSplit(PacemanPaceMilestone lastMilestone)
    {
        if (Data.ItemsData.EstimatedCounts != null)
        {
            Data.ItemsData.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
            Data.ItemsData.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
            if (Data.Splits.Count > 2 && !Inventory.DisplayItems) Inventory.DisplayItems = true;

            Inventory.BlazeRodsCount = estimatedRods;
            Inventory.PearlsCount = estimatedPearls;
        }

        if (Data.Splits.Count > 1 && (lastMilestone.SplitName.Equals("enter_bastion") || lastMilestone.SplitName.Equals("enter_fortress")))
        {
            SplitType = Data.Splits[^2].SplitName.Equals("rsg.enter_nether") ? SplitType.structure_1 : SplitType.structure_2;
        }
        else
        {
            SplitType = Enum.Parse<SplitType>(lastMilestone.SplitName);
        }

        SetPacePriority(Service.CheckForGoodPace(SplitType, lastMilestone));
        CurrentSplitTimeMiliseconds = lastMilestone.IGT;
        LastMilestone = lastMilestone;
    }
    private void UpdateIGTTime()
    {
        IGTTimeMiliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - Data.LastUpdate + LastMilestone!.IGT;
    }

    public PacemanPaceMilestone GetLastSplit()
    {
        return GetSplit(1)!;
    }
    public PacemanPaceMilestone? GetSplit(int indexFromEnd)
    {
        if (indexFromEnd > Data.Splits.Count) return null;
        var lastSplit = Data.Splits[^indexFromEnd];
        return new PacemanPaceMilestone()
        {
            SplitName = lastSplit.SplitName,
            RTA = lastSplit.RTA,
            IGT = lastSplit.IGT,
        };
    }

    private void SetPacePriority(bool good)
    {
        IsPacePrioritized = good;
    }
}