using System.Net.Http;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using TournamentTool.Components.Controls;
using TournamentTool.ViewModels;
using System.Windows;
using TournamentTool.Utils;
using System.Windows.Media;
using System.ComponentModel;

namespace TournamentTool.Models;

public enum SplitType
{
    none,
    enter_nether,
    structure_1,
    structure_2,
    first_portal,
    second_portal,
    enter_stronghold,
    enter_end,
    credits
}

public class PaceMan : BaseViewModel, IPlayer, IPace
{
    private ControllerViewModel? Controller { get; set; }

    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceSplitsList> Splits { get; set; } = [];

    [JsonPropertyName("itemData")]
    public PaceItemData ItemsData { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public long LastUpdate { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("isCheated")]
    public bool IsCheated { get; set; }

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
            OnPropertyChanged(nameof(SplitName));
        }
    }
    public string? SplitName { get; set; }

    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            _currentSplitTimeMiliseconds = value;
            TimeSpan time = TimeSpan.FromMilliseconds(_currentSplitTimeMiliseconds);
            CurrentSplitTime = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
            OnPropertyChanged(nameof(CurrentSplitTime));
        }
    }
    public string CurrentSplitTime { get; set; } = "00:00";

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


    public void Initialize(ControllerViewModel controller, List<PaceSplitsList> splits)
    {
        Controller = controller;

        UpdateHeadImage();
        UpdateTime(splits);
    }

    public void Update(PaceMan paceman)
    {
        User = paceman.User;
        Splits = paceman.Splits;
        LastUpdate = paceman.LastUpdate;
        ItemsData = paceman.ItemsData;
        IsCheated = paceman.IsCheated;
        IsHidden = paceman.IsHidden;

        if (Splits == null || Splits.Count == 0) return;
        UpdateTime(Splits);
    }

    private void UpdateTime(List<PaceSplitsList> splits)
    {
        for (int i = 0; i < splits.Count; i++)
            splits[i].SplitName = splits[i].SplitName.Replace("rsg.", "");
        PaceSplitsList lastSplit = splits[^1];

        if (ItemsData.EstimatedCounts != null)
        {
            ItemsData.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
            ItemsData.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
            if ((estimatedPearls > 0 || estimatedRods > 0) && !Inventory.DisplayItems) Inventory.DisplayItems = true;

            Inventory.BlazeRodsCount = estimatedRods;
            Inventory.PearlsCount = estimatedPearls;
        }

        if (splits.Count > 1 && (lastSplit.SplitName.Equals("enter_bastion") || lastSplit.SplitName.Equals("enter_fortress")))
        {
            if (splits[^2].SplitName.Equals("enter_nether"))
                SplitType = SplitType.structure_1;
            else
                SplitType = SplitType.structure_1;
        }
        else
            SplitType = (SplitType)Enum.Parse(typeof(SplitType), lastSplit.SplitName);

        CheckForGoodPace(lastSplit);

        CurrentSplitTimeMiliseconds = lastSplit.IGT;
        IGTTimeMiliseconds = ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - LastUpdate) + lastSplit.IGT;
    }

    private void UpdateHeadImage()
    {
        if (HeadImage != null) return;

        if (Player == null)
        {
            string url = $"https://minotar.net/helm/{User.UUID}/180.png";
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

    public string GetDisplayName()
    {
        return Player == null ? Nickname : Player.GetDisplayName();
    }
    public string GetPersonalBest()
    {
        return Player == null ? "Unk" : Player.GetPersonalBest();
    }
    public string GetTwitchName()
    {
        return User.TwitchName;
    }
    public string GetHeadViewParametr()
    {
        return Player == null ? User.UUID : Player.GetHeadViewParametr();
    }
    public bool IsFromWhiteList()
    {
        return Player != null;
    }

    public bool IsLive()
    {
        return !string.IsNullOrEmpty(User.TwitchName);
    }

    private void CheckForGoodPace(PaceSplitsList lastSplit)
    {
        if (Controller == null) return;

        switch (SplitType)
        {
            case SplitType.structure_2:
                SetPacePriority(Controller.Configuration.Structure2GoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.first_portal:
                SetPacePriority(Controller.Configuration.FirstPortalGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_stronghold:
                SetPacePriority(Controller.Configuration.EnterStrongholdGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_end:
                SetPacePriority(Controller.Configuration.EnterEndGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.credits:
                SetPacePriority(Controller.Configuration.CreditsGoodPaceMiliseconds > lastSplit.IGT);
                break;
            default:
                SetPacePriority(false);
                break;
        }
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

    private void PlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        //To jest jeden ze sposobow, zeby zrobic realtime identyfikator w povie dla pacemana z odwolaniem do whitelisty, ale mi sie totalnie nie podoba
        if (e.PropertyName == nameof(Player.IsUsedInPov))
        {
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }
}

public class PaceSplitsList
{
    [JsonPropertyName("eventId")]
    public string SplitName { get; set; } = string.Empty;

    [JsonPropertyName("rta")]
    public long RTA { get; set; }

    [JsonPropertyName("igt")]
    public long IGT { get; set; }
}

public class PaceManUser
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }

    [JsonPropertyName("liveAccount")]
    public string TwitchName { get; set; }
}

public struct PaceManStreamData
{
    [JsonPropertyName("twitchId")]
    public string MainID { get; set; }

    [JsonPropertyName("alt")]
    public string AltID { get; set; }
}

public struct PaceItemData
{
    [JsonPropertyName("estimatedCounts")]
    public Dictionary<string, int> EstimatedCounts { get; set; }

    [JsonPropertyName("usages")]
    public Dictionary<string, int> Usages { get; set; }
}
