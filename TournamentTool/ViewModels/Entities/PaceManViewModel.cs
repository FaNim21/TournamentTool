using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Services.Background;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class PaceManViewModel : BaseViewModel, IPlayer, IPace
{
    private Paceman _paceman;

    public SplitType ModelSplitType => _paceman.SplitType;
    public long ModelCurrentSplitTimeMiliseconds => _paceman.CurrentSplitTimeMiliseconds;
    
    public string DisplayName => _paceman.PlayerViewModel == null ? _paceman.Data.Nickname : _paceman.PlayerViewModel.DisplayName;
    public string GetPersonalBest => _paceman.PlayerViewModel == null ? "Unk" : _paceman.PlayerViewModel.GetPersonalBest;
    public string HeadViewParameter => _paceman.PlayerViewModel == null ? _paceman.Data.User.UUID! : _paceman.PlayerViewModel.HeadViewParameter;
    public string TwitchName => _paceman.Data.User.TwitchName ?? string.Empty;
    public bool IsFromWhitelist => _paceman.PlayerViewModel != null;
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

    private BitmapImage? _headImage;
    public BitmapImage? HeadImage
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
    
    private Brush? _statusLabelColor = new SolidColorBrush(Consts.DefaultColor);
    public Brush? StatusLabelColor
    {
        get => _statusLabelColor;
        set
        {
            _statusLabelColor = value;
            OnPropertyChanged(nameof(StatusLabelColor));
        }
    }
    
    public Brush? PaceSplitTimeColor { get; set; }
    public FontWeight PaceFontWeight { get; set; } = FontWeights.Normal;
    
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
        get => _paceman.PlayerViewModel?.IsUsedInPov ?? false;
        set
        {
            if (_paceman.PlayerViewModel == null) return;

            _paceman.PlayerViewModel.IsUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }
    public bool IsUsedInPreview { get; set; }

    private bool _isPacePrioritized;
    
    
    public PaceManViewModel(Paceman paceman)
    {
        _paceman = paceman;
        Inventory = new PlayerInventoryViewModel(_paceman.Inventory);
        OnPropertyChanged(nameof(Inventory));
        
        Initialize();
    }
    public void Initialize()
    {
        SetPacePriority(false);
        InGameName = _paceman.Nickname;
        Update();
        
        Application.Current?.Dispatcher.Invoke(delegate
        {
            if (_paceman.Data.ShowOnlyLive)
            {
                StatusLabelColor = new SolidColorBrush(Consts.DefaultColor);
                return;
            }
            
            StatusLabelColor = IsLive ? new SolidColorBrush(Consts.LiveColor) : new SolidColorBrush(Consts.OfflineColor);
        });
    }

    public void Update()
    {
        CheckForPacePriority();

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