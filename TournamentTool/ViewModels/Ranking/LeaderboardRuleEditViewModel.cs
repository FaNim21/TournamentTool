using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Lua;

namespace TournamentTool.ViewModels.Ranking;

public class LuaLeaderboardScriptViewModel : BaseViewModel
{
    private readonly LuaLeaderboardScriptEntry _data;

    public string Name => _data.Name;
    public string FullPath => _data.FullPath;
    public string Description => _data.Description;
    public string Version => _data.Version;
    public IReadOnlyList<LuaCustomVariable> CustomVariables => _data.CustomVariables;

    
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
    private readonly ILuaScriptsManager _luaScriptsManager;
    private readonly INotifyPresetModification _notifyPresetModification;
    private readonly LeaderboardRule _ruleModel;
    
    private LeaderboardRuleViewModel? _rule;
    public LeaderboardRuleViewModel Rule
    {
        get => _rule!;
        init
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
            _selectedSubRule?.SetupCustomVariables();
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


    public LeaderboardRuleEditViewModel(LeaderboardRuleViewModel rule, ILuaScriptsManager luaScriptsManager, INotifyPresetModification notifyPresetModification)
    {
        Rule = rule;
        _ruleModel = rule.GetLeaderboardRule();

        _luaScriptsManager = luaScriptsManager;
        _notifyPresetModification = notifyPresetModification;

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
                subRule.SetupScript(LuaScripts.FirstOrDefault(s => s.Name.Equals(subRule.LuaPath)));
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
            var subRuleViewModel = new LeaderboardSubRuleViewModel(subRule, Rule, _notifyPresetModification);
            subRuleViewModel.SelectedScript = LuaScripts[0];
            Rule.SubRules.Add(subRuleViewModel);
            _notifyPresetModification.PresetIsModified();
        });
    }

    public void RemoveSubRule(LeaderboardSubRuleViewModel subRule)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _ruleModel.SubRules.Remove(subRule.Model);
            Rule.SubRules.Remove(subRule);
            _notifyPresetModification.PresetIsModified();
        });
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