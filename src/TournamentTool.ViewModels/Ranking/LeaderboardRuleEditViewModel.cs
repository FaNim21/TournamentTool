using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities.Lua;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Leaderboard;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public class LuaLeaderboardScriptViewModel : BaseViewModel
{
    private readonly LuaLeaderboardScriptEntry _data;

    public string Name => _data.Name;
    public string FullPath => _data.FullPath;
    public string Description => _data.Description;
    public string Version => _data.Version;
    public IReadOnlyList<LuaCustomVariable> CustomVariables => _data.CustomVariables;

    
    public LuaLeaderboardScriptViewModel(LuaLeaderboardScriptEntry data, IDispatcherService dispatcher) : base(dispatcher)
    {
        _data = data;
        
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(FullPath));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Version));
    }
}

public class LeaderboardRuleEditWindowViewModel : BaseViewModel
{
    private readonly INotifyPresetModification _notifyPresetModification;
    private readonly IDialogService _dialogService;
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
            _selectedSubRule?.OnDisable();
            _selectedSubRule = value;
            _selectedSubRule?.OnEnable(null);
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


    public LeaderboardRuleEditWindowViewModel(LeaderboardRuleViewModel rule, ILuaScriptsManager luaScriptsManager, INotifyPresetModification notifyPresetModification, IDialogService dialogService, IDispatcherService dispatcher) : base(dispatcher)
    {
        Rule = rule;
        _ruleModel = rule.GetLeaderboardRule();

        _notifyPresetModification = notifyPresetModification;
        _dialogService = dialogService;

        Dispatcher.Invoke(() =>
        {
            LuaScripts.Clear();
            foreach (var script in luaScriptsManager.GetScriptsList())
            {
                var scriptViewModel = new LuaLeaderboardScriptViewModel(script, dispatcher);
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
        Dispatcher.Invoke(() =>
        {
            _ruleModel.SubRules.Add(subRule);
            var subRuleViewModel = new LeaderboardSubRuleViewModel(subRule, Rule, _notifyPresetModification, _dialogService, Dispatcher);
            subRuleViewModel.SelectedScript = LuaScripts[0];
            Rule.SubRules.Add(subRuleViewModel);
            _notifyPresetModification.MarkAsModified();
        });
    }

    public void RemoveSubRule(LeaderboardSubRuleViewModel subRule)
    {
        Dispatcher.Invoke(() =>
        {
            _ruleModel.SubRules.Remove(subRule.Model);
            Rule.SubRules.Remove(subRule);
            _notifyPresetModification.MarkAsModified();
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