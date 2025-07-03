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
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Ranking;

/// <summary>
/// Tutaj trzeba bedzie trigerowac tez skrypty Lua pod punktacje
/// </summary>
public class LeaderboardRuleViewModel : BaseViewModel
{
    private readonly LeaderboardRule _rule;

    public string Name
    {
        get => _rule.Name;
        set
        {
            _rule.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    public bool IsEnabled
    {
        get => _rule.IsEnabled;
        set
        {
            _rule.IsEnabled = value;
            OnPropertyChanged(nameof(IsEnabled));
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                BrushIsEnabledColor = value ? new SolidColorBrush(Consts.LiveColor) : new SolidColorBrush(Consts.OfflineColor);
            });
            OnPropertyChanged(nameof(BrushIsEnabledColor));
            
            IsEnabledText = value ? "1": "0";
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
        }
    }

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

    public ObservableCollection<LeaderboardSubRuleViewModel> SubRules { get; set; } = [];

    public List<RunMilestone> FilteredMilestones { get; set; } = [];

    public ICommand SwitchRuleTypeCommand { get; set; }


    public LeaderboardRuleViewModel(LeaderboardRule rule)
    {
        _rule = rule;

        IsEnabled = _rule.IsEnabled;

        SwitchRuleTypeCommand = new RelayCommand(SwitchRuleType);
        _chosenCopy = ChosenMilestone;

        foreach (var subRule in rule.SubRules)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SubRules.Add(new LeaderboardSubRuleViewModel(subRule, this));
            });
        }
        RuleTypeText = RuleType.ToString();
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
        ChosenMilestone = attr!.RuleType.Equals(RuleType) || attr.RuleType.Equals(LeaderboardRuleType.All) ? _chosenCopy : RunMilestone.None;
        
        FilteredMilestones = new List<RunMilestone>(filtered);
        OnPropertyChanged(nameof(FilteredMilestones));
    }
    
    private void SwitchRuleType()
    {
        RuleType = RuleType == LeaderboardRuleType.Split ? LeaderboardRuleType.Advancement : LeaderboardRuleType.Split;
        FilterSplitsAndAdvancements(ControllerMode);
    }
    
    public LeaderboardRule GetLeaderboardRule() => _rule;
}