using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public enum RankedSplitType
{
    None,

}

public class RankedPace : BaseViewModel, IPlayer, IPace
{
    private string _inGameName { get; set; }
    public string InGameName
    {
        get => _inGameName;
        set
        {
            _inGameName = value;
            OnPropertyChanged(nameof(InGameName));
        }
    }

    private int _eloRate { get; set; }
    public int EloRate
    {
        get => _eloRate;
        set
        {
            _eloRate = value;
            OnPropertyChanged(nameof(EloRate));
        }
    }

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

    private RankedSplitType _splitType;
    public RankedSplitType SplitType
    {
        get => _splitType;
        set
        {
            if (_splitType == value) return;

            _splitType = value;
            SplitName = value.ToString().Replace('_', ' ').CaptalizeAll();
            OnPropertyChanged(nameof(SplitType));
            OnPropertyChanged(nameof(SplitName));
        }
    }
    public string? SplitName { get; set; }

    private List<string> _timelines;
    public List<string> Timelines
    {
        get => _timelines;
        set
        {
            _timelines = value;
            OnPropertyChanged(nameof(Timelines));
        }
    }

    private string _lastTimeline;
    public string LastTimeline
    {
        get => _lastTimeline;
        set
        {
            _lastTimeline = value;
            OnPropertyChanged(nameof(LastTimeline));
        }
    }


    public RankedPace()
    {
        
    }

    public string GetDisplayName()
    {
        throw new NotImplementedException();
    }

    public string GetHeadViewParametr()
    {
        throw new NotImplementedException();
    }

    public string GetPersonalBest()
    {
        throw new NotImplementedException();
    }

    public string GetTwitchName()
    {
        throw new NotImplementedException();
    }

    public bool IsFromWhiteList()
    {
        throw new NotImplementedException();
    }
}
