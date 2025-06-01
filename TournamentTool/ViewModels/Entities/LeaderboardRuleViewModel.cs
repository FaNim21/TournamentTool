using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Attributes;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

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

    public ObservableCollection<LeaderboardSubRuleViewModel> SubRules { get; set; } = [];

    public List<RunMilestone> FilteredMilestones { get; set; } = [];

    public ICommand SwitchRuleTypeCommand { get; set; }

    private ControllerMode _controllerMode;
    
    
    public LeaderboardRuleViewModel(LeaderboardRule rule)
    {
        _rule = rule;

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
        _controllerMode = controllerMode;
        
        IEnumerable<RunMilestone> filtered = Enum.GetValues<RunMilestone>()
            .Where(mode =>
            {
                var memberInfo = typeof(RunMilestone).GetMember(mode.ToString()).FirstOrDefault();
                var attr = memberInfo?.GetCustomAttribute<EnumRuleContextAttribute>();
                if (attr == null) return false;

                bool ruleMatch = RuleType.Equals(attr.RuleType) || attr.RuleType == LeaderboardRuleType.All;
                bool modeMatch = attr.ControllerMode == _controllerMode || attr.ControllerMode == ControllerMode.All;
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
        FilterSplitsAndAdvancements(_controllerMode);
    }
    
    public LeaderboardRule GetLeaderboardRule() => _rule;
}