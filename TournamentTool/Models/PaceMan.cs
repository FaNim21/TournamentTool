﻿using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using TournamentTool.ViewModels;
using TournamentTool.Utils;

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
    public ControllerViewModel? Controller { get; set; }

    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceSplitsList> Splits { get; set; } = [];

    [JsonPropertyName("contextEventList")]
    public List<PaceSplitsList> Advacements { get; set; } = [];

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


    public void Initialize(ControllerViewModel controller)
    {
        Controller = controller;

        UpdateHeadImage();
        UpdateTime();
    }

    public void Update(PaceMan paceman)
    {
        //TODO: 1 that shit is crazy and about to go down
        //to jest tak slabe ze az mi sie chce plakac dlaczego lenistwo jest silniejsze
        //trzeba to zrobic klasykiem model-viewmodel
        User = paceman.User;
        Splits = paceman.Splits;
        LastUpdate = paceman.LastUpdate;
        ItemsData = paceman.ItemsData;
        IsCheated = paceman.IsCheated;
        IsHidden = paceman.IsHidden;

        if (Splits.Count == 0) return;
        UpdateTime();
    }

    private void UpdateTime()
    {
        foreach (var t in Splits)
            t.SplitName = t.SplitName.Replace("rsg.", "");

        PaceSplitsList lastSplit = Splits[^1];

        if (ItemsData.EstimatedCounts != null)
        {
            ItemsData.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
            ItemsData.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
            if (Splits.Count > 2 && !Inventory.DisplayItems) Inventory.DisplayItems = true;

            Inventory.BlazeRodsCount = estimatedRods;
            Inventory.PearlsCount = estimatedPearls;
        }

        if (Splits.Count > 1 && (lastSplit.SplitName.Equals("enter_bastion") || lastSplit.SplitName.Equals("enter_fortress")))
        {
            SplitType = Splits[^2].SplitName.Equals("enter_nether") ? SplitType.structure_1 : SplitType.structure_2;
        }
        else
        {
            SplitType = Enum.Parse<SplitType>(lastSplit.SplitName);
        }

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
        return User.TwitchName!;
    }
    public string GetHeadViewParametr()
    {
        return Player == null ? User.UUID! : Player.GetHeadViewParametr();
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
                SetPacePriority(Controller.TournamentViewModel.Structure2GoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.first_portal:
                SetPacePriority(Controller.TournamentViewModel.FirstPortalGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_stronghold:
                SetPacePriority(Controller.TournamentViewModel.EnterStrongholdGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_end:
                SetPacePriority(Controller.TournamentViewModel.EnterEndGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.credits:
                SetPacePriority(Controller.TournamentViewModel.CreditsGoodPaceMiliseconds > lastSplit.IGT);
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
    public string? UUID { get; set; }

    [JsonPropertyName("liveAccount")]
    public string? TwitchName { get; set; }
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
