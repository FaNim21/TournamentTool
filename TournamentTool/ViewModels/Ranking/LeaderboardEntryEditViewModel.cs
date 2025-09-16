using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using TournamentTool.Attributes;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Factories;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardEntryEditViewModel : BaseViewModel
{
    private readonly TournamentViewModel _tournament;
    private readonly LeaderboardPanelViewModel _leaderboardPanelViewModel;
    private readonly LeaderboardEntry _originalEntry;
    private readonly PlayerViewModel? _player;
    private readonly INotifyPresetModification _notifyPresetModification;

    public LeaderboardEntryViewModel? EditedEntry { get; private set; }
    
    public List<RunMilestone> FilteredMilestones { get; set; } = [];
    
    private ControllerMode _controllerMode;
    public ControllerMode ControllerMode
    {
        get => _controllerMode;
        set
        {
            _controllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
        }
    }

    private string _titleInfo = string.Empty;
    public string TitleInfo
    {
        get => _titleInfo;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "Click new to create or right click edit";
            }
            _titleInfo = value;
            OnPropertyChanged(nameof(TitleInfo));
        }
    }

    private bool _milestoneType;
    public bool MilestoneType
    {
        get => _milestoneType;
        set
        {
            _milestoneType = value;
            OnPropertyChanged(nameof(MilestoneType));
        }
    }

    private LeaderboardRuleType _ruleType = LeaderboardRuleType.Split;
    public LeaderboardRuleType RuleType
    {
        get => _ruleType;
        set
        {
            _ruleType = value;
            OnPropertyChanged(nameof(RuleType));
        }
    }
    
    private RunMilestone _mainChosenMilestone;
    public RunMilestone MainChosenMilestone
    {
        get => _mainChosenMilestone;
        set
        {
            _mainChosenMilestone = value; 
            OnPropertyChanged(nameof(MainChosenMilestone));
        }
    }

    private RunMilestone _previousChosenMilestone;
    public RunMilestone PreviousChosenMilestone
    {
        get => _previousChosenMilestone;
        set
        {
            _previousChosenMilestone = value; 
            OnPropertyChanged(nameof(PreviousChosenMilestone));
        }
    }

    private int _mainTime;
    public int MainTime
    {
        get => _mainTime;
        set
        {
            _mainTime = value;
            OnPropertyChanged(nameof(MainTime));

            MainTimeText = TimeSpan.FromMilliseconds(value).ToFormattedTime();
            OnPropertyChanged(nameof(MainTimeText));
        }
    }
    public string MainTimeText { get; private set; } = string.Empty;

    public int _previousTime;
    public int PreviousTime
    {
        get => _previousTime;
        set
        {
            _previousTime = value;
            OnPropertyChanged(nameof(PreviousTime));
            
            PreviousTimeText = TimeSpan.FromMilliseconds(value).ToFormattedTime();
            OnPropertyChanged(nameof(PreviousTimeText));
        }
    }
    public string PreviousTimeText { get; private set; } = string.Empty;

    private int _points;
    public int Points
    {
        get => _points;
        set
        {
            _points = value;
            OnPropertyChanged(nameof(Points));
        }
    }
    
    private int _round;
    public int Round
    {
        get => _round;
        set
        {
            _round = value;
            OnPropertyChanged(nameof(Round));
        }
    }

    private string _worldID = string.Empty;
    public string WorldID
    {
        get => _worldID;
        set
        {
            _worldID = value;
            OnPropertyChanged(nameof(WorldID));
        }
    }
    
    public ICommand SaveCommand { get; private set; }
    public ICommand RollbackCommand { get; private set; }
    
    public ICommand NewMilestoneCommand { get; private set; }
    public ICommand SetMilestoneCommand { get; private set; }

    public ICommand SwitchRuleTypeCommand { get; private set; }

    public ICommand EditMilestoneCommand { get; private set; }
    public ICommand RemoveMilestoneCommand { get; private set; }

    private int _currentEditedMilestone = -1;
    private bool _isSaved = true;
    private bool _madeChanges = false;
    

    public LeaderboardEntryEditViewModel(TournamentViewModel tournament,
        LeaderboardPanelViewModel leaderboardPanelViewModel, LeaderboardEntry originalEntry, PlayerViewModel? player,
        INotifyPresetModification notifyPresetModification)
    {
        _tournament = tournament;
        _leaderboardPanelViewModel = leaderboardPanelViewModel;
        _originalEntry = originalEntry;
        _player = player;
        _notifyPresetModification = notifyPresetModification;
        DeepCopyToEdited();

        SaveCommand = new RelayCommand(Save);
        RollbackCommand = new RelayCommand(Rollback);
        
        NewMilestoneCommand = new RelayCommand(NewMilestone);
        SetMilestoneCommand = new RelayCommand(SetMilestone);
        
        SwitchRuleTypeCommand = new RelayCommand(SwitchRuleType);

        EditMilestoneCommand = new RelayCommand<IEntryMilestoneDataViewModel>(EditMilestone);
        RemoveMilestoneCommand = new RelayCommand<IEntryMilestoneDataViewModel>(RemoveMilestone);
    }
    public override bool OnDisable()
    {
        if (!_isSaved) return false;
        if (_madeChanges)
        {
            _notifyPresetModification.PresetIsModified();
        }
        _leaderboardPanelViewModel.RecalculateAllEntries();
        return true;
    }

    public void SetPresetFilters(ControllerMode controllerMode)
    {
        ControllerMode = controllerMode;
        
        IEnumerable<RunMilestone> filtered = Enum.GetValues<RunMilestone>()
            .Where(mode =>
            {
                var memberInfo = typeof(RunMilestone).GetMember(mode.ToString()).FirstOrDefault();
                var attr = memberInfo?.GetCustomAttribute<EnumRuleContextAttribute>();
                if (attr == null) return false;

                bool ruleMatch = RuleType.Equals(attr.RuleType) || attr.RuleType == LeaderboardRuleType.All;
                bool modeMatch = attr.ControllerMode == ControllerMode || attr.ControllerMode == ControllerMode.All;
                return ruleMatch && modeMatch;
            });

        MainChosenMilestone = RunMilestone.None;
        PreviousChosenMilestone = RunMilestone.None;
        
        FilteredMilestones = new List<RunMilestone>(filtered);
        OnPropertyChanged(nameof(FilteredMilestones));
    }
    private void SwitchRuleType()
    {
        RuleType = RuleType == LeaderboardRuleType.Split ? LeaderboardRuleType.Advancement : LeaderboardRuleType.Split;
        SetPresetFilters(ControllerMode);
    }

    private void NewMilestone()
    {
        Clear();
        TitleInfo = "Creating new milestone entry";
    }
    private void SetMilestone()
    {
        if (EditedEntry == null) return;
        if (MainChosenMilestone == RunMilestone.None)
        {
            DialogBox.Show("You need to at least set main milestone type", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        bool isEdited = TitleInfo.StartsWith("editing", StringComparison.CurrentCultureIgnoreCase);
        MessageBoxResult result = isEdited
            ? DialogBox.Show($"Are you sure you want to apply those milestone changes?", "Applying edited milestone data", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : DialogBox.Show($"Are you sure you want to add new milestone?", "Adding new milestone", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        EntryMilestoneData milestoneData;
        LeaderboardTimeline main = new(MainChosenMilestone, MainTime);
        LeaderboardTimeline? previous = new(PreviousChosenMilestone, PreviousTime);
        if (PreviousChosenMilestone == RunMilestone.None) previous = null;
        
        if (MilestoneType)
        {
            milestoneData = new EntryRankedMilestoneData(main, previous, Points, Round);
        }
        else
        {
            milestoneData = new EntryPacemanMilestoneData(main, previous, Points, WorldID);
        }

        if (isEdited)
        {
            EditedEntry.Data.Milestones[_currentEditedMilestone] = milestoneData;
        }
        else
        {
            bool success = EditedEntry.Data.AddMilestone(milestoneData);
            if (!success)
            {
                DialogBox.Show($"Couldn't add that new milestone because same one already exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        
        EditedEntry.SetupOpeningWindow();
        _isSaved = false;
        Clear();
    }
    private void Clear()
    {
        RuleType = LeaderboardRuleType.Split;
        MainChosenMilestone = RunMilestone.None;
        MainTime = 0;
        PreviousChosenMilestone = RunMilestone.None;
        PreviousTime = 0;
        Points = 0;
        Round = 0;
        WorldID = string.Empty;
        TitleInfo = string.Empty;
        _currentEditedMilestone = -1;
    }
    
    private void EditMilestone(IEntryMilestoneDataViewModel milestone)
    {
        if (milestone is EntryMilestonePacemanDataViewModel paceman)
        {
            MainChosenMilestone = paceman.Data.Main.Milestone;
            MainTime = paceman.Data.Main.Time;
            if (paceman.Data.Previous == null)
            {
                PreviousChosenMilestone = RunMilestone.None;
                PreviousTime = 0;
            }
            else
            {
                PreviousChosenMilestone = paceman.Data.Previous.Milestone;
                PreviousTime = paceman.Data.Previous.Time;
            }
            Points = paceman.Data.Points;
            Round = 0;
            WorldID = paceman.Data.WorldID;
            MilestoneType = false;
        }
        else if (milestone is EntryMilestoneRankedDataViewModel ranked)
        {
            MainChosenMilestone = ranked.Data.Main.Milestone;
            MainTime = ranked.Data.Main.Time;
            if (ranked.Data.Previous == null)
            {
                PreviousChosenMilestone = RunMilestone.None;
                PreviousTime = 0;
            }
            else
            {
                PreviousChosenMilestone = ranked.Data.Previous.Milestone;
                PreviousTime = ranked.Data.Previous.Time;
            }
            Points = ranked.Data.Points;
            WorldID = string.Empty;
            Round = ranked.Data.Round;
            MilestoneType = true;
        }
        
        _currentEditedMilestone = EditedEntry!.Milestones.IndexOf(milestone);
        TitleInfo = $"Editing milestone at index: {_currentEditedMilestone}";
    }
    private void RemoveMilestone(IEntryMilestoneDataViewModel milestone)
    {
        bool isEdited = TitleInfo.StartsWith("editing", StringComparison.CurrentCultureIgnoreCase);
        if (isEdited) return;
        
        int index = EditedEntry!.Milestones.IndexOf(milestone);
        EditedEntry.Milestones.RemoveAt(index);
        EditedEntry.Data.Milestones.RemoveAt(index);
        _isSaved = false;
    }
    
    private void Save()
    {
        string ign = EditedEntry?.Player?.InGameName ?? string.Empty;
        if (DialogBox.Show($"Are you sure you want to save?\nYou will apply all changes for {ign} entry\nThis will mark this entry as edited", "Rolling back changes", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        if (EditedEntry == null) return;
        
        _isSaved = true;
        _madeChanges = true;
        _originalEntry.Milestones.Clear();
        _originalEntry.BestMilestones.Clear();
        _originalEntry.Points = 0;
        _originalEntry.IsEdited = true;
        foreach (var milestone in EditedEntry.Data.Milestones)
        {
            _originalEntry.AddMilestone(milestone);
        }
    }
    private void Rollback()
    {
        if (DialogBox.Show("Are you sure you want to rollback?\nYou will lose all changes", "Rolling back changes", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        DeepCopyToEdited();
        _isSaved = true;
    }

    private void DeepCopyToEdited()
    {
        var duplicatedEntry = new LeaderboardEntry
        {
            PlayerUUID = _originalEntry.PlayerUUID,
            Points = _originalEntry.Points,
            Position = _originalEntry.Position,
        };

        foreach (var milestone in _originalEntry.Milestones)
        {
            EntryMilestoneData duplicatedMilestone = LeaderboardEntryMilestoneFactory.DeepCopy(milestone);
            duplicatedEntry.Milestones.Add(duplicatedMilestone);
        }
        
        EditedEntry = new LeaderboardEntryViewModel(duplicatedEntry, _player);
        OnPropertyChanged(nameof(EditedEntry));
        EditedEntry.SetupOpeningWindow();
        Clear();
    }
}