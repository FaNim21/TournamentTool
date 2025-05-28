using System.Diagnostics;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

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

    
    public LeaderboardSubRuleViewModel(LeaderboardSubRule model, LeaderboardRuleViewModel rule)
    {
        Model = model;
        Rule = rule;
        
        Time = Model.Time;
    }

    public bool Evaluate(LeaderboardPlayerEvaluateData data)
    {
        if (data.Time > Time) return false;
        
        var span = TimeSpan.FromMilliseconds(data.Time);
        string timeText = $"{(int)span.TotalMinutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}"; 
        if (data.PlayerViewModel == null)
            Trace.WriteLine($"Player: ??? just achieved milestone: \"{Rule.ChosenMilestone}\" in time: {timeText}, so under {TimeText}");
        else
            Trace.WriteLine($"Player: \"{data.PlayerViewModel.InGameName}\" just achieved milestone: \"{Rule.ChosenMilestone}\" in time: {timeText}, so under {TimeText}");
        
        return true;
    }
}