using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Selectable.Controller.SidePanel;

namespace TournamentTool.ViewModels.Entities;

public class PaceManViewModel : BaseViewModel, IGroupableItem, IPlayer, IPace
{
    private Paceman _paceman;
    
    public string GroupKey => SplitName!;
    public int GroupSortOrder => (int)SplitType;
    public long SortValue => CurrentSplitTimeMiliseconds;
    public string SecondarySortValue => InGameName;
    public string Identifier => InGameName;

    public SplitType ModelSplitType => _paceman.SplitType;
    public long ModelCurrentSplitTimeMiliseconds => _paceman.CurrentSplitTimeMiliseconds;

    public string DisplayName => _paceman.Player == null ? _paceman.Nickname : _paceman.Player.Name ?? "Unk";
    public string GetPersonalBest => _paceman.Player == null ? "Unk" : _paceman.Player.PersonalBest;
    public string HeadViewParameter => _paceman.Player == null ? _paceman.UUID! : _paceman.Player.InGameName ?? string.Empty;
    public StreamDisplayInfo StreamDisplayInfo => _paceman.StreamDisplayInfo ?? new StreamDisplayInfo("", StreamType.twitch);
    public bool IsFromWhitelist => _paceman.Player != null;

    public bool IsLive => _paceman.IsLive;

    public PlayerInventoryViewModel Inventory { get; set; }

    private string _inGameName = string.Empty;
    public string InGameName
    {
        get => _inGameName;
        set
        {
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }
    
    private string? _splitName = string.Empty;
    public string? SplitName
    {
        get => _splitName;
        set
        {
            _splitName = value;
            OnPropertyChanged(nameof(SplitName));
        }
    }
    
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

    private object? _headImage;
    public object? HeadImage
    {
        get => _headImage;
        set
        {
            if (_headImage == value) return;
            _headImage = value;
            OnPropertyChanged(nameof(HeadImage));
        } 
    }

    private float _headImageOpacity;
    public float HeadImageOpacity
    {
        get => _headImageOpacity;
        set
        {
            if (Math.Abs(_headImageOpacity - value) < 0.1f) return;
            _headImageOpacity = value;
            OnPropertyChanged(nameof(HeadImageOpacity));
        } 
    }
    
    private string _statusLabelColor = Consts.DefaultColor;
    public string StatusLabelColor
    {
        get => _statusLabelColor;
        set
        {
            _statusLabelColor = value;
            OnPropertyChanged(nameof(StatusLabelColor));
        }
    }
    
    public string? PaceSplitTimeColor { get; set; }
    public FontWeight PaceFontWeight { get; set; } = FontWeight.Normal;
    
    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            if (CurrentSplitTimeMiliseconds == value) return;
            _currentSplitTimeMiliseconds = value;
            OnPropertyChanged(nameof(CurrentSplitTimeMiliseconds));
            
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
            if (IGTTimeMiliseconds == value) return;
            _igtTimeMiliseconds = value;
            OnPropertyChanged(nameof(IGTTimeMiliseconds));
            
            TimeSpan time = TimeSpan.FromMilliseconds(_igtTimeMiliseconds);
            IGTTime = $"{time.Minutes:D2}:{time.Seconds:D2}";
            OnPropertyChanged(nameof(IGTTime));
        }
    }
    public string IGTTime { get; set; } = "00:00";
    
    public bool IsUsedInPov
    {
        get => _paceman.Player?.IsUsedInPov ?? false;
        set
        {
            //TODO: 9 to jest pod znakiem zapytanie z racji nei oznaczania graczy, ktorzy nie sa z whitelisty
            if (_paceman.Player == null) return;

            _paceman.Player.IsUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }
    public bool IsUsedInPreview { get; set; }

    private bool _isPacePrioritized;
    
    
    public PaceManViewModel() : base(null!) { }
    public PaceManViewModel(Paceman paceman, IDispatcherService dispatcher) : base(dispatcher)
    {
        _paceman = paceman;
        Inventory = new PlayerInventoryViewModel(_paceman.Inventory, dispatcher);
        OnPropertyChanged(nameof(Inventory));
        
        Initialize();
    }

    public void Initialize()
    {
        SetPacePriority(false);
        InGameName = _paceman.Nickname;
        Update();
        
        UpdateLiveInfo();
    }

    public void Update()
    {
        CheckForPacePriority();
        UpdateLiveInfo();

        HeadImage = _paceman.HeadImage;
        HeadImageOpacity = _paceman.HeadImageOpacity;
        
        SplitType = _paceman.SplitType;
        CurrentSplitTimeMiliseconds = _paceman.CurrentSplitTimeMiliseconds;
        IGTTimeMiliseconds = _paceman.IGTTimeMiliseconds;
        
        Inventory.Update();
    }

    private void CheckForPacePriority()
    {
        if (_isPacePrioritized == _paceman.IsPacePrioritized) return;
        
        SetPacePriority(_paceman.IsPacePrioritized);
        _isPacePrioritized = _paceman.IsPacePrioritized;
    }
    
    private void SetPacePriority(bool good)
    {
        if (good)
        {
            PaceFontWeight = FontWeight.Bold;
            PaceSplitTimeColor = Consts.GoldPaceColor;
        }
        else
        {
            PaceFontWeight = FontWeight.Thin;
            PaceSplitTimeColor = Consts.NormalPaceColor;
        }

        OnPropertyChanged(nameof(PaceSplitTimeColor));
        OnPropertyChanged(nameof(PaceFontWeight));
    }
    private void UpdateLiveInfo()
    {
        StatusLabelColor = IsLive ? Consts.LiveColor : Consts.OfflineColor;
    }
}