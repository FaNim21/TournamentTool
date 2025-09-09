using System.Collections.ObjectModel;
using TournamentTool.Models;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;

namespace TournamentTool.ViewModels.Entities;

public class RankedBestSplitDataViewModel : BaseViewModel
{
    private readonly PrivRoomBestSplitData _splitData;
    
    public string? PlayerName => _splitData.PlayerName;
    public long Time => _splitData.Time;
    public long TimeDifference => _splitData.TimeDifference;

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
    
    private string _timeDifferenceText = string.Empty;
    public string TimeDifferenceText
    {
        get => _timeDifferenceText;
        set
        {
            _timeDifferenceText = value;
            OnPropertyChanged(nameof(TimeDifferenceText));
        } 
    }

    
    public RankedBestSplitDataViewModel(PrivRoomBestSplitData splitData)
    {
        _splitData = splitData;
        
        TimeText = TimeSpan.FromMilliseconds(_splitData.Time).ToFormattedTime();
        TimeDifferenceText = _splitData.TimeDifference == 0 ? string.Empty : " +" + TimeSpan.FromMilliseconds(_splitData.TimeDifference).ToFormattedTime();
        OnPropertyChanged(nameof(PlayerName));
        OnPropertyChanged(nameof(Time));
        OnPropertyChanged(nameof(TimeDifference));
    }
}

public class RankedBestSplitViewModel : BaseViewModel
{
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

    public ObservableCollection<RankedBestSplitDataViewModel> Splits { get; } = [];


    public RankedBestSplitViewModel(PrivRoomBestSplit bestSplit)
    {
        TypeName = bestSplit.Type.ToString();
    }
}