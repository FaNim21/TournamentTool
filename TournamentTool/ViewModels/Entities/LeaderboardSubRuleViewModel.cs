using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardSubRuleViewModel : BaseViewModel
{
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
        } 
    }
    public int Time
    {
        get => Model.Time;
        set
        {
            Model.Time = value;
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

    
    public LeaderboardSubRuleViewModel(LeaderboardSubRule model)
    {
        Model = model;
    }
}