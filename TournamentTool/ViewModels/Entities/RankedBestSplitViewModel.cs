using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;

namespace TournamentTool.ViewModels.Entities;

public class RankedBestSplitViewModel : BaseViewModel
{
    private readonly PrivRoomBestSplit _bestSplit;
    
    public string? PlayerName
    {
        get => _bestSplit.PlayerName;
        set
        {
            _bestSplit.PlayerName = value;
            OnPropertyChanged(nameof(PlayerName));
        }
    }
    public RankedSplitType Type 
    {
        get => _bestSplit.Type;
        set
        {
            _bestSplit.Type = value;
            TypeName = _bestSplit.Type.ToString();
            OnPropertyChanged(nameof(Type));
        }
    }
    public long Time 
    {
        get => _bestSplit.Time;
        set
        {
            _bestSplit.Time = value;
            TimeText = TimeSpan.FromMilliseconds(value).ToFormattedTime();
            OnPropertyChanged(nameof(Time));
        }
    }
    
    private string _typeName = string.Empty;
    public string TypeName
    {
        get => _typeName;
        set
        {
            _typeName = value.Replace('_', ' ').CaptalizeAll();
            OnPropertyChanged(nameof(TypeName));
        } 
    }

    private string _timeText = string.Empty;
    public string TimeText
    {
        get => _timeText;
        set
        {
            _timeText = value;
            OnPropertyChanged(nameof(TimeText));
        } 
    }


    public RankedBestSplitViewModel(PrivRoomBestSplit bestSplit)
    {
        _bestSplit = bestSplit;

        ReloadProperties();
    }

    private void ReloadProperties()
    {
        PlayerName = _bestSplit.PlayerName;
        Type = _bestSplit.Type;
        Time = _bestSplit.Time;
    }
}