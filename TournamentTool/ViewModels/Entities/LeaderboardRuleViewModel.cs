using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class LeaderboardRuleViewModel : BaseViewModel
{
    private readonly LeaderboardRule _rule;

    public string Name
    {
        get => _rule.Name;
        set
        {
            _rule.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }


    public LeaderboardRuleViewModel(LeaderboardRule rule)
    {
        _rule = rule;
    }

    public LeaderboardRule GetLeaderboardRule() => _rule;
}
