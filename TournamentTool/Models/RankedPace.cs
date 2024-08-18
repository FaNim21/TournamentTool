using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public class RankedPace : BaseViewModel, IPlayer
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
