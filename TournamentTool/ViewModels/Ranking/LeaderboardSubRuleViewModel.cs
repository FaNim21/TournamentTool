using System.Diagnostics;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardSubRuleViewModel : BaseViewModel
{
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
        } 
    }
    public int Time
    {
        get => Model.Time;
        set
        {
            Model.Time = value;
            TimeText = TimeSpan.FromMilliseconds(value).ToFormattedTime();
            OnPropertyChanged(nameof(Time));
        } 
    }
    public int BasePoints
    {
        get => Model.BasePoints;
        set
        {
            Model.BasePoints = value;
            OnPropertyChanged(nameof(BasePoints));
        } 
    }
    public int MaxWinners
    {
        get => Model.MaxWinners;
        set
        {
            Model.MaxWinners = value;
            OnPropertyChanged(nameof(MaxWinners));
        } 
    }
    public bool Repeatable
    {
        get => Model.Repeatable;
        set
        {
            Model.Repeatable = value;
            OnPropertyChanged(nameof(Repeatable));
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
        }
    }


    public LeaderboardSubRuleViewModel(LeaderboardSubRule model, LeaderboardRuleViewModel rule)
    {
        Model = model;
        Rule = rule;
        
        Time = Model.Time;
    }
}