using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class LeaderboardRuleEditViewModel : BaseViewModel
{
    private readonly LeaderboardRule _ruleModel;
    
    private LeaderboardRuleViewModel _rule;
    public LeaderboardRuleViewModel Rule
    {
        get => _rule;
        set
        {
            _rule = value;
            OnPropertyChanged(nameof(Rule));
        }
    }

    private LeaderboardSubRuleViewModel? _selectedSubRule;
    public LeaderboardSubRuleViewModel? SelectedSubRule
    {
        get => _selectedSubRule;
        set
        {
            _selectedSubRule = value;
            OnPropertyChanged(nameof(SelectedSubRule));
            
            IsGeneralVisible = false;
        }
    }

    private bool _isGeneralVisible = true;
    public bool IsGeneralVisible
    {
        get => _isGeneralVisible;
        set
        {
            _isGeneralVisible = value;
            OnPropertyChanged(nameof(IsGeneralVisible));
        }
    }

    public ICommand OpenGeneralCommand { get; set; }

    public ICommand AddSubRuleCommand { get; set; }
    public ICommand RemoveSubRuleCommand { get; set; }


    public LeaderboardRuleEditViewModel(LeaderboardRuleViewModel rule)
    {
        _rule = rule;
        OnPropertyChanged(nameof(Rule));
        _ruleModel = rule.GetLeaderboardRule();

        OpenGeneralCommand = new RelayCommand(OpenGeneralConfigRule);
        
        AddSubRuleCommand = new RelayCommand(AddSubRule);
        RemoveSubRuleCommand = new RemoveSubRuleCommand(this);
    }

    private void OpenGeneralConfigRule()
    {
        SelectedSubRule = null;
        IsGeneralVisible = true;
    }

    private void AddSubRule()
    {
        LeaderboardSubRule subRule = new();
        Application.Current.Dispatcher.Invoke(() =>
        {
            _ruleModel.SubRules.Add(subRule);
            Rule.SubRules.Add(new LeaderboardSubRuleViewModel(subRule));
        });
    }

    public void RemoveSubRule(LeaderboardSubRuleViewModel subRule)
    {
        _ruleModel.SubRules.Remove(subRule.Model);
        Rule.SubRules.Remove(subRule);
    }

}