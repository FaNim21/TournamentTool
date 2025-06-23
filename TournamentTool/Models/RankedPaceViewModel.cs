using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using TournamentTool.Enums;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class RankedPaceViewModel : BaseViewModel, IPlayer, IPace
{
    public RankedPace Data { get; }

    public PlayerInventoryViewModel Inventory { get; }

    public string DisplayName => Data.Player == null ? InGameName : Data.Player.DisplayName;
    public string GetPersonalBest => Data.Player == null ? "Unk" : Data.Player.GetPersonalBest;
    public string HeadViewParameter => Data.Player == null ? InGameName : Data.Player.HeadViewParameter;
    public string TwitchName => Data.Player == null ? string.Empty : Data.Player.TwitchName;
    public bool IsFromWhitelist => Data.Player != null;
    public bool IsLive => Data.IsLive;
    
    private bool _isUsedInPov;
    public bool IsUsedInPov
    {
        get => _isUsedInPov; 
        set
        {
            _isUsedInPov = value;
            OnPropertyChanged(nameof(IsUsedInPov));
        }
    }

    private bool _isUsedInPreview;
    public bool IsUsedInPreview
    {
        get => _isUsedInPreview;
        set
        {
            _isUsedInPreview = value;
            OnPropertyChanged(nameof(IsUsedInPreview));
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

    private string _inGameName = string.Empty;
    public string InGameName
    {
        get => _inGameName;
        set
        {
            if (_inGameName == value) return;
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
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

    private RankedSplitType _splitType;
    public RankedSplitType SplitType
    {
        get => _splitType;
        set
        {
            if (_splitType == value) return;
            _splitType = value;
            OnPropertyChanged(nameof(SplitType));
            SplitName = value.ToString().Replace('_', ' ').CaptalizeAll();
        }
    }

    private string? _splitName = string.Empty;
    public string? SplitName
    {
        get => _splitName;
        set
        {
            if (_splitName == value) return;
            _splitName = value;
            OnPropertyChanged(nameof(SplitName));
        } 
    }

    private string _lastTimeline = string.Empty;
    public string LastTimeline
    {
        get => _lastTimeline;
        set
        {
            if (_lastTimeline == value) return;
            _lastTimeline = value;
            OnPropertyChanged(nameof(LastTimeline));
        }
    }

    private long _currentSplitTimeMiliseconds;
    public long CurrentSplitTimeMiliseconds
    {
        get => _currentSplitTimeMiliseconds;
        set
        {
            if (_currentSplitTimeMiliseconds == value) return;
            _currentSplitTimeMiliseconds = value;
            CurrentSplitTime = TimeSpan.FromMilliseconds(value).ToSimpleFormattedTime();
        } 
    }
    private string _currentSplitTime = "00:00";
    public string CurrentSplitTime
    {
        get => _currentSplitTime;
        set
        {
            _currentSplitTime = value;
            OnPropertyChanged(nameof(CurrentSplitTime));
        } 
    }

    private long _differenceSplitTimeMiliseconds;
    public long DifferenceSplitTimeMiliseconds
    {
        get => _differenceSplitTimeMiliseconds;
        set
        {
            if (_differenceSplitTimeMiliseconds == value) return;
            _differenceSplitTimeMiliseconds = value;
            SplitDifferenceTime = TimeSpan.FromMilliseconds(value).ToSimpleFormattedTime("+");
        } 
    }
    private string _splitDifferenceTime = "00:00";
    public string SplitDifferenceTime
    {
        get => _splitDifferenceTime;
        set
        {
            _splitDifferenceTime = value;
            OnPropertyChanged(nameof(SplitDifferenceTime));
        } 
    }


    public RankedPaceViewModel(RankedPace data)
    {
        Data = data;
        Inventory = new PlayerInventoryViewModel(data.Inventory);
        OnPropertyChanged(nameof(Inventory));
        
        Update();
    }

    public void Update()
    {
        HeadImage = Data.HeadImage;
        HeadImageOpacity = Data.HeadImageOpacity;
        
        InGameName = Data.InGameName;
        SplitType = Data.SplitType;
        LastTimeline = Data.LastTimeline;
        
        CurrentSplitTimeMiliseconds = Data.CurrentSplitTimeMiliseconds;
        DifferenceSplitTimeMiliseconds = Data.DifferenceSplitTimeMiliseconds;
        
        Inventory.Update();
    }
}
