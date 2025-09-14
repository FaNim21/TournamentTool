using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors.Media;
using TournamentTool.Attributes;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardRuleViewModel : BaseViewModel
{
    private readonly LeaderboardRule _rule;
    private readonly INotifyPresetModification _notifyPresetModification;

    public string Name
    {
        get => _rule.Name;
        set
        {
            _rule.Name = value;
            OnPropertyChanged(nameof(Name));
            _notifyPresetModification.PresetIsModified();
        }
    }
    public bool IsEnabled
    {
        get => _rule.IsEnabled;
        set
        {
            _rule.IsEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
            
            SetIsEnabledButton();
            _notifyPresetModification.PresetIsModified();
        }
    }
    public LeaderboardRuleType RuleType
    {
        get => _rule.RuleType;
        set
        {
            _rule.RuleType = value;
            RuleTypeText = value.ToString();
            OnPropertyChanged(nameof(RuleType));
            _notifyPresetModification.PresetIsModified();
        }
    }

    private RunMilestone _chosenCopy;
    public RunMilestone ChosenMilestone
    {
        get => _rule.ChosenAdvancement;
        set
        {
            if (ChosenMilestone != RunMilestone.None)
            {
                _chosenCopy = _rule.ChosenAdvancement;
            }
            
            _rule.ChosenAdvancement = value; 
            OnPropertyChanged(nameof(ChosenMilestone));
            _notifyPresetModification.PresetIsModified();
        }
    }

    public Brush? BrushBorderFocused { get; set; } = Brushes.Gray;
    public Brush? BrushIsEnabledColor { get; set; }
    
    private string _isEnabledText = string.Empty;
    public string IsEnabledText
    {
        get => _isEnabledText;
        set
        {
            _isEnabledText = value == "0" ? "Off" : "On";
            OnPropertyChanged(nameof(IsEnabledText));
        } 
    }

    private string _ruleTypeText = string.Empty;
    public string RuleTypeText
    {
        get => _ruleTypeText;
        set
        {
            _ruleTypeText = value;
            OnPropertyChanged(nameof(RuleTypeText));
        }
    }
    
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

    private bool _isCollapsed;
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            _isCollapsed = value;
            OnPropertyChanged(nameof(IsCollapsed));
        }
    }

    private string _collapseButtonName = ">";
    public string CollapseButtonName
    {
        get => _collapseButtonName;
        set
        {
            _collapseButtonName = value;
            OnPropertyChanged(nameof(CollapseButtonName));
        }
    }

    private bool _isFocused;
    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            _isFocused = value;
            Application.Current.Dispatcher.Invoke(() =>
            {
                BrushBorderFocused = value ? Brushes.LightBlue : Brushes.Gray;
                OnPropertyChanged(nameof(BrushBorderFocused));
            });
        }
    }

    public ObservableCollection<LeaderboardSubRuleViewModel> SubRules { get; set; } = [];

    public List<RunMilestone> FilteredMilestones { get; set; } = [];

    public ICommand SwitchRuleTypeCommand { get; set; }
    public ICommand ShowCollapseCommand { get; set; }


    public LeaderboardRuleViewModel(LeaderboardRule rule, INotifyPresetModification notifyPresetModification)
    {
        _rule = rule;
        _notifyPresetModification = notifyPresetModification;

        SwitchRuleTypeCommand = new RelayCommand(SwitchRuleType);
        ShowCollapseCommand = new RelayCommand(SwitchSubRulesVisibility);
        
        SetIsEnabledButton();
        _chosenCopy = ChosenMilestone;

        foreach (var subRule in rule.SubRules)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SubRules.Add(new LeaderboardSubRuleViewModel(subRule, this, notifyPresetModification));
            });
        }
        RuleTypeText = RuleType.ToString();
    }

    private void SetIsEnabledButton()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            BrushIsEnabledColor = IsEnabled ? new SolidColorBrush(Consts.LiveColor) : new SolidColorBrush(Consts.OfflineColor);
        });
        OnPropertyChanged(nameof(BrushIsEnabledColor));
            
        IsEnabledText = IsEnabled ? "1": "0";
    }

    public void FilterSplitsAndAdvancements(ControllerMode controllerMode)
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

        var memberInfo = typeof(RunMilestone).GetMember(ChosenMilestone.ToString()).FirstOrDefault();
        var attr = memberInfo?.GetCustomAttribute<EnumRuleContextAttribute>();
        var milestone = attr!.RuleType.Equals(RuleType) || attr.RuleType.Equals(LeaderboardRuleType.All) ? _chosenCopy : RunMilestone.None;
        
        if (ChosenMilestone != RunMilestone.None)
        {
            _chosenCopy = _rule.ChosenAdvancement;
        }
        _rule.ChosenAdvancement = milestone; 
        OnPropertyChanged(nameof(ChosenMilestone));
        
        FilteredMilestones = new List<RunMilestone>(filtered);
        OnPropertyChanged(nameof(FilteredMilestones));
    }
    
    private void SwitchRuleType()
    {
        RuleType = RuleType == LeaderboardRuleType.Split ? LeaderboardRuleType.Advancement : LeaderboardRuleType.Split;
        FilterSplitsAndAdvancements(ControllerMode);
        _notifyPresetModification.PresetIsModified();
    }
    private void SwitchSubRulesVisibility()
    {
        IsCollapsed = !IsCollapsed;
        CollapseButtonName = IsCollapsed ? "v" : ">";
    }
    
    public LeaderboardRule GetLeaderboardRule() => _rule;
}