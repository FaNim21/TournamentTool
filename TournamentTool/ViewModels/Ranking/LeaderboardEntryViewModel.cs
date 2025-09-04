using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public abstract class EntryMilestoneDataViewModel : BaseViewModel;
public class EntryMilestoneRankedDataViewModel : EntryMilestoneDataViewModel
{
    private readonly EntryRankedMilestoneData _data;

    public RunMilestone MainMilestoneType => _data.Main.Milestone;
    public string MainMilestoneTime => TimeSpan.FromMilliseconds(_data.Main.Time).ToFormattedTime();
    
    public RunMilestone PreviousMilestoneType => _data.Previous!.Milestone;
    public string PreviousMilestoneTime => TimeSpan.FromMilliseconds(_data.Previous!.Time).ToFormattedTime();

    public int Round => _data.Round;
    public int Points => _data.Points;

    
    public EntryMilestoneRankedDataViewModel(EntryRankedMilestoneData data)
    {
        _data = data;
        
        OnPropertyChanged(nameof(MainMilestoneType));
        OnPropertyChanged(nameof(MainMilestoneTime));
        OnPropertyChanged(nameof(PreviousMilestoneType));
        OnPropertyChanged(nameof(PreviousMilestoneTime));
        OnPropertyChanged(nameof(Round));
        OnPropertyChanged(nameof(Points));
    }
}
public class EntryMilestonePacemanDataViewModel : EntryMilestoneDataViewModel
{
    private readonly EntryPacemanMilestoneData _data;

    public RunMilestone MainMilestoneType => _data.Main.Milestone;
    public string MainMilestoneTime => TimeSpan.FromMilliseconds(_data.Main.Time).ToFormattedTime();
    
    public RunMilestone PreviousMilestoneType => _data.Previous!.Milestone;
    public string PreviousMilestoneTime => TimeSpan.FromMilliseconds(_data.Previous!.Time).ToFormattedTime();

    public string WorldID => _data.WorldID;
    public int Points => _data.Points;


    public EntryMilestonePacemanDataViewModel(EntryPacemanMilestoneData data)
    {
        _data = data;
        
        OnPropertyChanged(nameof(MainMilestoneType));
        OnPropertyChanged(nameof(MainMilestoneTime));
        OnPropertyChanged(nameof(PreviousMilestoneType));
        OnPropertyChanged(nameof(PreviousMilestoneTime));
        OnPropertyChanged(nameof(WorldID));
        OnPropertyChanged(nameof(Points));
    }
}

public class LeaderboardEntryViewModel : BaseViewModel
{
    private readonly LeaderboardEntry _entry;

    public PlayerViewModel? Player { get; }

    private ObservableCollection<EntryMilestoneDataViewModel> _milestones = [];
    public ObservableCollection<EntryMilestoneDataViewModel> Milestones
    {
        get => _milestones;
        set
        {
            _milestones = value;
            OnPropertyChanged(nameof(Milestones));
        } 
    }

    public string PlayerUUID => _entry.PlayerUUID;
    public int Points
    {
        get => _entry.Points;
        set
        {
            _entry.Points = value;
            OnPropertyChanged(nameof(Points));
        }
    }
    public int Position
    {
        get => _entry.Position;
        set
        {
            _entry.Position = value;
            OnPropertyChanged(nameof(Position));
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


    public LeaderboardEntryViewModel(LeaderboardEntry entry, PlayerViewModel? player)
    {
        _entry = entry;
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
    }
    public void RefreshBestMilestone(RunMilestone milestone)
    {
        var data = _entry.GetBestMilestone(milestone);
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
        Helper.StartProcess($"https://paceman.gg/stats/run/{worldID}");
    }
    
    public void SetupOpeningWindow()
    {
        foreach (var milestones in _entry.Milestones.Batch(5))
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

    public LeaderboardEntry GetLeaderboardEntry() => _entry;
}