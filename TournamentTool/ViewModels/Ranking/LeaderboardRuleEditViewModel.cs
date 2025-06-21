using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Ranking;

public class LuaLeaderboardScriptViewModel : BaseViewModel
{
    private readonly LuaLeaderboardScriptEntry _data;

    public string Name => _data.Name;
    public string FullPath => _data.FullPath;
    public string Description => _data.Description;
    public string Version => _data.Version;

    
    public LuaLeaderboardScriptViewModel(LuaLeaderboardScriptEntry data)
    {
        _data = data;
        
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FullPath));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Version));
    }
}

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
            
            IsGeneralVisible = value == null;
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

    private ObservableCollection<LuaLeaderboardScriptViewModel> _luaScripts = [];
    public ObservableCollection<LuaLeaderboardScriptViewModel> LuaScripts
    {
        get => _luaScripts;
        set
        {
            _luaScripts = value;
            OnPropertyChanged(nameof(LuaScripts));
        }
    }

    public ICommand OpenGeneralCommand { get; set; }

    public ICommand AddSubRuleCommand { get; set; }
    public ICommand RemoveSubRuleCommand { get; set; }
    
    public ICommand MoveSubRuleCommand { get; set; }


    public LeaderboardRuleEditViewModel(LeaderboardRuleViewModel rule, ILuaScriptsManager luaScriptsManager)
    {
        _rule = rule;
        OnPropertyChanged(nameof(Rule));
        _ruleModel = rule.GetLeaderboardRule();

        Application.Current.Dispatcher.Invoke(() =>
        {
            LuaScripts.Clear();
            foreach (var script in luaScriptsManager.GetScriptsList())
            {
                var scriptViewModel = new LuaLeaderboardScriptViewModel(script);
                LuaScripts.Add(scriptViewModel);
            }

            foreach (var subRule in Rule.SubRules)
            {
                subRule.SelectedScript = LuaScripts.FirstOrDefault(s => s.Name.Equals(subRule.LuaPath));
            }
        });

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
            var subRuleViewModel = new LeaderboardSubRuleViewModel(subRule, Rule);
            subRuleViewModel.SelectedScript = LuaScripts[0];
            Rule.SubRules.Add(subRuleViewModel);
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
        _ruleModel.Move(oldIndex, newIndex);
    }
}