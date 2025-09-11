using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public interface IEntryMilestoneDataViewModel;
public class EntryMilestoneDataViewModel<T> : BaseViewModel, IEntryMilestoneDataViewModel where T : EntryMilestoneData
{
    public T Data { get; }

    public RunMilestone MainMilestoneType => Data.Main.Milestone;
    public string MainMilestoneTime => TimeSpan.FromMilliseconds(Data.Main.Time).ToFormattedTime();

    public bool IsPreviousEmpty => Data.Previous == null;
    public string PreviousMilestoneType
    {
        get
        {
            if (Data.Previous == null) return string.Empty;
            return Data.Previous.Milestone.ToString() ?? string.Empty;
        }
    }
    public string PreviousMilestoneTime
    {
        get
        {
            if (Data.Previous == null) return string.Empty;
            return TimeSpan.FromMilliseconds(Data.Previous.Time).ToFormattedTime();
        }
    }

    public int Points => Data.Points;


    protected EntryMilestoneDataViewModel(T data)
    {
        Data = data;
        
        OnPropertyChanged(nameof(MainMilestoneType));
        OnPropertyChanged(nameof(MainMilestoneTime));
        OnPropertyChanged(nameof(IsPreviousEmpty));
        OnPropertyChanged(nameof(PreviousMilestoneType));
        OnPropertyChanged(nameof(PreviousMilestoneTime));
        OnPropertyChanged(nameof(Points));
    }
}

public class EntryMilestoneRankedDataViewModel : EntryMilestoneDataViewModel<EntryRankedMilestoneData>
{
    public int Round => Data.Round;
    
    public EntryMilestoneRankedDataViewModel(EntryRankedMilestoneData data) : base(data)
    {
        OnPropertyChanged(nameof(Round));
    }
}
public class EntryMilestonePacemanDataViewModel : EntryMilestoneDataViewModel<EntryPacemanMilestoneData>
{
    public string WorldID => Data.WorldID;

    public EntryMilestonePacemanDataViewModel(EntryPacemanMilestoneData data) : base(data)
    {
        OnPropertyChanged(nameof(WorldID));
    }
}

public class LeaderboardEntryViewModel : BaseViewModel
{
    public LeaderboardEntry Data { get; }

    public PlayerViewModel? Player { get; }

    private ObservableCollection<IEntryMilestoneDataViewModel> _milestones = [];
    public ObservableCollection<IEntryMilestoneDataViewModel> Milestones
    {
        get => _milestones;
        set
        {
            _milestones = value;
            OnPropertyChanged(nameof(Milestones));
        } 
    }
    
    public string PlayerUUID => Data.PlayerUUID;
    public int Points
    {
        get => Data.Points;
        set
        {
            Data.Points = value;
            OnPropertyChanged(nameof(Points));
        }
    }
    public int Position
    {
        get => Data.Position;
        set
        {
            Data.Position = value;
            OnPropertyChanged(nameof(Position));
        }
    }
    public bool IsEdited
    {
        get => Data.IsEdited;
        set
        {
            Data.IsEdited = value;
            OnPropertyChanged(nameof(IsEdited));
        }
    }
    
    private string _bestTimeOnPrioritizeMilestone = string.Empty;
    public string BestTimeOnPrioritizeMilestone
    {
        get => _bestTimeOnPrioritizeMilestone;
        set
        {
            _bestTimeOnPrioritizeMilestone = value;
            OnPropertyChanged(nameof(BestTimeOnPrioritizeMilestone));
        }
    }

    private string _averageTimeOnPrioritizeMilestone = string.Empty;
    public string AverageTimeOnPrioritizeMilestone
    {
        get => _averageTimeOnPrioritizeMilestone;
        set
        {
            _averageTimeOnPrioritizeMilestone = value;
            OnPropertyChanged(nameof(AverageTimeOnPrioritizeMilestone));
        }
    }

    public ICommand OpenPacemanWorldIDCommand { get; private set; }


    public LeaderboardEntryViewModel(LeaderboardEntry data, PlayerViewModel? player)
    {
        Data = data;
        Player = player;
        Player?.UpdateHeadBitmap();

        OpenPacemanWorldIDCommand = new RelayCommand<string>(OpenPacemanWorldID);
    }

    public void Refresh(RunMilestone milestone = RunMilestone.None)
    {
        if (milestone != RunMilestone.None)
        {
            RefreshBestMilestone(milestone);
        }
            
        OnPropertyChanged(nameof(Points));
        OnPropertyChanged(nameof(Position));
        OnPropertyChanged(nameof(IsEdited));
    }
    public void RefreshBestMilestone(RunMilestone milestone)
    {
        var data = Data.GetBestMilestone(milestone);
        if (data == null)
        {
            BestTimeOnPrioritizeMilestone = "Unknown";
            AverageTimeOnPrioritizeMilestone = "Unknown"; 
            return;
        }

        BestTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(data.BestTime).ToFormattedTime();
        AverageTimeOnPrioritizeMilestone = TimeSpan.FromMilliseconds(data.Average).ToFormattedTime() + $" ({data.Amount})"; 
    }

    private void OpenPacemanWorldID(string worldID)
    {
        if (string.IsNullOrEmpty(worldID)) return;
        Helper.StartProcess($"https://paceman.gg/stats/run/{worldID}");
    }
    
    public void SetupOpeningWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Milestones.Clear();
        });
        
        foreach (var milestones in Data.Milestones.Batch(5))
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var milestone in milestones)
                {
                    switch (milestone)
                    {
                        case EntryRankedMilestoneData ranked:
                            Milestones.Add(new EntryMilestoneRankedDataViewModel(ranked));
                            break;
                        case EntryPacemanMilestoneData paceman:
                            Milestones.Add(new EntryMilestonePacemanDataViewModel(paceman));
                            break;
                    }
                }
            }, DispatcherPriority.Background);
        }
    }
    public override void Dispose()
    {
        Milestones.Clear();
    }

    public LeaderboardEntry GetLeaderboardEntry() => Data;
}