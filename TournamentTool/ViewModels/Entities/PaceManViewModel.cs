using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class PaceManViewModel : BaseViewModel, IPlayer, IPace
{
    private PaceMan _paceMan;
    
    private TournamentViewModel? TournamentViewModel { get; set; }

    public string Nickname => _paceMan.Nickname;
    public string TwitchName => _paceMan.User.TwitchName ?? string.Empty;

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

    
    public PaceManViewModel(PaceMan paceMan, TournamentViewModel? tournamentViewModel, Player player)
    {
        _paceMan = paceMan;
        TournamentViewModel = tournamentViewModel;
        Player = player;
        
        UpdateHeadImage();
        UpdateTime();
    }
    
    public void Update(PaceMan paceman)
    {
        //TODO: 1 that shit is crazy and about to go down
        //to jest tak slabe ze az mi sie chce plakac dlaczego lenistwo jest silniejsze
        //trzeba to zrobic klasykiem model-viewmodel

        _paceMan = paceman;
        
        if (_paceMan.Splits.Count == 0) return;
        UpdateTime();
    }
    private void UpdateTime()
    {
        foreach (var t in _paceMan.Splits)
            t.SplitName = t.SplitName.Replace("rsg.", "");

        PaceSplitsList lastSplit = _paceMan.Splits[^1];

        if (_paceMan.ItemsData.EstimatedCounts != null)
        {
            _paceMan.ItemsData.EstimatedCounts.TryGetValue("minecraft:ender_pearl", out int estimatedPearls);
            _paceMan.ItemsData.EstimatedCounts.TryGetValue("minecraft:blaze_rod", out int estimatedRods);
            if (_paceMan.Splits.Count > 2 && !Inventory.DisplayItems) Inventory.DisplayItems = true;

            Inventory.BlazeRodsCount = estimatedRods;
            Inventory.PearlsCount = estimatedPearls;
        }

        if (_paceMan.Splits.Count > 1 && (lastSplit.SplitName.Equals("enter_bastion") || lastSplit.SplitName.Equals("enter_fortress")))
        {
            SplitType = _paceMan.Splits[^2].SplitName.Equals("enter_nether") ? SplitType.structure_1 : SplitType.structure_2;
        }
        else
        {
            SplitType = Enum.Parse<SplitType>(lastSplit.SplitName);
        }

        CheckForGoodPace(lastSplit);

        CurrentSplitTimeMiliseconds = lastSplit.IGT;
        IGTTimeMiliseconds = ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _paceMan.LastUpdate) + lastSplit.IGT;
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

    public string GetDisplayName()
    {
        return Player == null ? _paceMan.Nickname : Player.GetDisplayName();
    }
    public string GetPersonalBest()
    {
        return Player == null ? "Unk" : Player.GetPersonalBest();
    }
    public string GetTwitchName()
    {
        return _paceMan.User.TwitchName!;
    }
    public string GetHeadViewParametr()
    {
        return Player == null ? _paceMan.User.UUID! : Player.GetHeadViewParametr();
    }
    public bool IsFromWhiteList()
    {
        return Player != null;
    }

    private void CheckForGoodPace(PaceSplitsList lastSplit)
    {
        if (TournamentViewModel == null) return;

        switch (SplitType)
        {
            case SplitType.structure_2:
                SetPacePriority(TournamentViewModel.Structure2GoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.first_portal:
                SetPacePriority(TournamentViewModel.FirstPortalGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_stronghold:
                SetPacePriority(TournamentViewModel.EnterStrongholdGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.enter_end:
                SetPacePriority(TournamentViewModel.EnterEndGoodPaceMiliseconds > lastSplit.IGT);
                break;
            case SplitType.credits:
                SetPacePriority(TournamentViewModel.CreditsGoodPaceMiliseconds > lastSplit.IGT);
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
}