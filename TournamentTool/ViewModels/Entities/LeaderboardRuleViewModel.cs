using System.Collections.ObjectModel;
using System.Windows;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

/// <summary>
/// Tutaj trzeba bedzie trigerowac tez skrypty Lua pod punktacje
/// </summary>
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
    public LeaderboardRuleType Type
    {
        get => _rule.Type;
        set
        {
            _rule.Type = value;
            OnPropertyChanged(nameof(Type));
        }
    }

    public ObservableCollection<LeaderboardSubRuleViewModel> SubRules { get; set; } = [];

    
    public LeaderboardRuleViewModel(LeaderboardRule rule)
    {
        _rule = rule;

        foreach (var subRule in rule.SubRules)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SubRules.Add(new LeaderboardSubRuleViewModel(subRule));
            });
        }
    }

    public void Evaluate()
    {
        
    }
    
    public LeaderboardRule GetLeaderboardRule() => _rule;
}
