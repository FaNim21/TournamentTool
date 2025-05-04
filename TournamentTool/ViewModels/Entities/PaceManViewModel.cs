using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Services;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class PaceManViewModel : BaseViewModel, IPlayer, IPace
{
    private PaceMan _paceMan;

    private PaceManService Service { get; set; }

    public string Nickname => _paceMan.Nickname;

    public Player? Player { get; set; }
    public PlayerInventory Inventory { get; set; } = new();

    private BitmapImage? _headImage;
    public BitmapImage? HeadImage
    {
        get => _headImage;
        set
        {
            _headImage = value;
            OnPropertyChanged(nameof(HeadImage));
        }
    }

    public string DisplayName => Player == null ? _paceMan.Nickname : Player.DisplayName;
    public string GetPersonalBest => Player == null ? "Unk" : Player.GetPersonalBest;
    public string HeadViewParameter => Player == null ? _paceMan.User.UUID! : Player.HeadViewParameter;
    public string TwitchName => _paceMan.User.TwitchName ?? string.Empty;
    public bool IsFromWhitelist => Player != null;
    
    public float HeadImageOpacity { get; set; }

    private SplitType _splitType;
    public SplitType SplitType
    {
        get => _splitType;
        set
        {
            if (SplitType == value) return;

            _splitType = value;
            SplitName = value.ToString().Replace('_', ' ').CaptalizeAll();
            OnPropertyChanged(nameof(SplitType));
        }
    }

    private string? _splitName;
    public string? SplitName
    {
        get => _splitName;
        set
        {
            _splitName = value;
            OnPropertyChanged(nameof(SplitName));
        }
    }

    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            _currentSplitTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_currentSplitTimeMiliseconds);
            CurrentSplitTime = $"{time.Minutes:D2}:{time.Seconds:D2}";
            OnPropertyChanged(nameof(CurrentSplitTime));
        }
    }
    public string CurrentSplitTime { get; set; } = "00:00";

    private PacemanPaceMilestone? LastMilestone { get; set; } = null;
    
    private long _igtTimeMiliseconds;
    public long IGTTimeMiliseconds
    {
        get => _igtTimeMiliseconds;
        set
        {
            _igtTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_igtTimeMiliseconds);
            IGTTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(IGTTime));
        }
    }
    public string IGTTime { get; set; } = "00:00";

    public Brush? PaceSplitTimeColor { get; set; }
    public FontWeight PaceFontWeight { get; set; } = FontWeights.Normal;

    public bool IsUsedInPov
    {
        get => Player?.IsUsedInPov ?? false;
        set
        {
            if (Player == null) return;

            Player.IsUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }

    public bool IsUsedInPreview { get; set; }

    
    public PaceManViewModel(PaceManService service, PaceMan paceMan, Player player)
    {
        Service = service;
        _paceMan = paceMan;
        Player = player;
        
        UpdateHeadImage();
        UpdateTime();
    }
    
    public void Update(PaceMan paceman)
    {
        _paceMan = paceman;
        
        if (_paceMan.Splits.Count == 0) return;
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

        if (Player == null)
        {
            string url = $"https://minotar.net/helm/{_paceMan.User.UUID}/180.png";
            HeadImageOpacity = 0.35f;
            Task.Run(async () =>
            {
                HeadImage = await Helper.LoadImageFromUrlAsync(url);
            });
        }
        else
        {
            HeadImageOpacity = 1f;
            HeadImage = Player!.Image;
        }
    }

    private void UpdateLastSplit(PacemanPaceMilestone lastMilestone)
    {
        if (_paceMan.ItemsData.EstimatedCounts != null)
        {
            _paceMan.ItemsData.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
            _paceMan.ItemsData.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
            if (_paceMan.Splits.Count > 2 && !Inventory.DisplayItems) Inventory.DisplayItems = true;

            Inventory.BlazeRodsCount = estimatedRods;
            Inventory.PearlsCount = estimatedPearls;
        }

        if (_paceMan.Splits.Count > 1 && (lastMilestone.SplitName.Equals("enter_bastion") || lastMilestone.SplitName.Equals("enter_fortress")))
        {
            SplitType = _paceMan.Splits[^2].SplitName.Equals("rsg.enter_nether") ? SplitType.structure_1 : SplitType.structure_2;
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
        IGTTimeMiliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - _paceMan.LastUpdate + LastMilestone!.IGT;
    }

    public PacemanPaceMilestone GetLastSplit()
    {
        var lastSplit = _paceMan.Splits[^1];
        return new PacemanPaceMilestone()
        {
            SplitName = lastSplit.SplitName,
            RTA = lastSplit.RTA,
            IGT = lastSplit.IGT,
        };
    }
    
    private void SetPacePriority(bool good)
    {
        if (good)
        {
            PaceFontWeight = FontWeights.Bold;
            Color gold = Color.FromRgb(255, 215, 0);
            Application.Current?.Dispatcher.Invoke(delegate { PaceSplitTimeColor = new SolidColorBrush(gold); });
        }
        else
        {
            PaceFontWeight = FontWeights.Thin;
            Color normal = Color.FromRgb(245, 222, 179);
            Application.Current?.Dispatcher.Invoke(delegate { PaceSplitTimeColor = new SolidColorBrush(normal); });
        }

        OnPropertyChanged(nameof(PaceSplitTimeColor));
        OnPropertyChanged(nameof(PaceFontWeight));
    }
}