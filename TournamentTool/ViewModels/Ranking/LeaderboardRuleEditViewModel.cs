using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Ranking;

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
    
    public ICommand MoveSubRuleCommand { get; set; }


    public LeaderboardRuleEditViewModel(LeaderboardRuleViewModel rule)
    {
        _rule = rule;
        OnPropertyChanged(nameof(Rule));
        _ruleModel = rule.GetLeaderboardRule();

        OpenGeneralCommand = new RelayCommand(OpenGeneralConfigRule);
        
        AddSubRuleCommand = new RelayCommand(AddSubRule);
        RemoveSubRuleCommand = new RemoveSubRuleCommand(this);
        
        MoveSubRuleCommand = new RelayCommand<(int oldIndex, int newIndex)>(MoveSubRuleItem);
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
            Rule.SubRules.Add(new LeaderboardSubRuleViewModel(subRule, Rule));
        });
    }

    public void RemoveSubRule(LeaderboardSubRuleViewModel subRule)
    {
        _ruleModel.SubRules.Remove(subRule.Model);
        Rule.SubRules.Remove(subRule);
    }

    private void MoveSubRuleItem((int oldIndex, int newIndex) indexTuple)
    {
        int oldIndex = indexTuple.oldIndex;
        int newIndex = indexTuple.newIndex;
        
        if (oldIndex < 0 || 
            newIndex < 0 || 
            oldIndex == newIndex || 
            oldIndex >= Rule.SubRules.Count ||
            newIndex >= Rule.SubRules.Count) return;
        
        Rule.SubRules.Move(oldIndex, newIndex);

        var item = _ruleModel.SubRules[oldIndex];
        _ruleModel.SubRules.RemoveAt(oldIndex);
        _ruleModel.SubRules.Insert(newIndex, item);
    }
}