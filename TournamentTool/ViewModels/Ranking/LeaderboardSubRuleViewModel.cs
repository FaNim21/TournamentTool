using System.Collections.ObjectModel;
using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils.Extensions;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardSubRuleViewModel : BaseViewModel
{
    private readonly INotifyPresetModification _notifyPresetModification;
    public LeaderboardRuleViewModel Rule { get; private set; }
    
    public LeaderboardSubRule Model { get; private set; }

    public string LuaPath
    {
        get => Model.LuaPath;
        set
        {
            Model.LuaPath = value;
            OnPropertyChanged(nameof(LuaPath));
        } 
    }
    public string Description
    {
        get => Model.Description;
        set
        {
            Model.Description = value;
            OnPropertyChanged(nameof(Description));
            _notifyPresetModification.PresetIsModified();
        } 
    }
    public int Time
    {
        get => Model.Time;
        set
        {
            Model.Time = value;
            OnPropertyChanged(nameof(Time));
            SetTime();
            _notifyPresetModification.PresetIsModified();
        } 
    }
    public int BasePoints
    {
        get => Model.BasePoints;
        set
        {
            Model.BasePoints = value;
            OnPropertyChanged(nameof(BasePoints));
            _notifyPresetModification.PresetIsModified();
        } 
    }
    public int MaxWinners
    {
        get => Model.MaxWinners;
        set
        {
            Model.MaxWinners = value;
            OnPropertyChanged(nameof(MaxWinners));
            _notifyPresetModification.PresetIsModified();
        } 
    }
    public bool Repeatable
    {
        get => Model.Repeatable;
        set
        {
            Model.Repeatable = value;
            OnPropertyChanged(nameof(Repeatable));
            _notifyPresetModification.PresetIsModified();
        } 
    }

    private string _timeText = string.Empty;
    public string TimeText
    {
        get => _timeText;
        set
        {
            _timeText = value;
            OnPropertyChanged(nameof(TimeText));
        } 
    }

    private LuaLeaderboardScriptViewModel? _selectedScript;
    public LuaLeaderboardScriptViewModel? SelectedScript
    {
        get => _selectedScript;
        set
        {
            if (value == null) return;
            
            _selectedScript = value;
            OnPropertyChanged(nameof(SelectedScript));

            LuaPath = _selectedScript.Name;
            _notifyPresetModification.PresetIsModified();
            
            Model.CustomVariables.Clear();
            foreach (var variable in _selectedScript.CustomVariables)
            {
                Model.CustomVariables[variable.Name] = variable;
            }
            SetupCustomVariables();
        }
    }

    public ObservableCollection<LuaCustomVariableViewModel> CustomVariables { get; private set; } = [];


    public LeaderboardSubRuleViewModel(LeaderboardSubRule model, LeaderboardRuleViewModel rule, INotifyPresetModification notifyPresetModification)
    {
        _notifyPresetModification = notifyPresetModification;
        
        Model = model;
        Rule = rule;

        SetTime();
    }
    private void SetTime()
    {
        TimeText = TimeSpan.FromMilliseconds(Time).ToFormattedTime();
    }
    
    public void SetupScript(LuaLeaderboardScriptViewModel? script)
    {
        if (script == null) return;
        
        _selectedScript = script;
        OnPropertyChanged(nameof(SelectedScript));

        LuaPath = script.Name;
        Model.UpdateCustomVariables(script.CustomVariables);
    }
    public void SetupCustomVariables()
    {
        CustomVariables = new ObservableCollection<LuaCustomVariableViewModel>(Model.CustomVariables.Values.Select(v => new LuaCustomVariableViewModel(v, _notifyPresetModification)));
        OnPropertyChanged(nameof(CustomVariables));
    }

    public void UpdateCustomVariablesValues()
    {
        foreach (var customVariable in CustomVariables)
        {
            customVariable.Update();
        }
    }
}