using System.Diagnostics;
using System.Windows.Input;
using OBSStudioClient.Classes;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.Utils.Extensions;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardSubRuleViewModel : BaseViewModel
{
    private readonly INotifyPresetModification _notifyPresetModification;
    public LeaderboardSubRule Model { get; private set; }

    public LeaderboardRuleViewModel Rule { get; private set; }

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
        }
    }


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
    public void SetupScriptOnOpen(LuaLeaderboardScriptViewModel? script)
    {
        if (script == null) return;
        
        _selectedScript = script;
        OnPropertyChanged(nameof(SelectedScript));

        LuaPath = script.Name;
    }
}