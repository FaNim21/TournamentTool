using System.Collections.ObjectModel;
using System.Windows;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public sealed class LeaderboardViewModel : BaseViewModel
{
    private readonly TournamentViewModel _tournamentViewModel;
    private readonly Tournament _tournament;

    private ObservableCollection<LeaderboardEntryViewModel> _entries = [];
    public ObservableCollection<LeaderboardEntryViewModel> Entries
    {
        get => _entries;
        set
        {
            _entries = value;
            OnPropertyChanged(nameof(Entries));
        }
    }
    
    private ObservableCollection<LeaderboardRuleViewModel> _rules = [];
    public ObservableCollection<LeaderboardRuleViewModel> Rules
    {
        get => _rules;
        set
        {
            _rules = value;
            OnPropertyChanged(nameof(Rules));
        }
    }
    
    
    public LeaderboardViewModel(Tournament tournament, TournamentViewModel tournamentViewModel)
    {
        _tournament = tournament;
        _tournamentViewModel = tournamentViewModel;
    }

    public void Setup()
    {
        foreach (var entry in _tournament.Leaderboard.Entries)
        {
            var player = _tournamentViewModel.GetPlayerByUUID(entry.PlayerUUID);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Entries.Add(new LeaderboardEntryViewModel(entry, player));
            });
        }
        
        foreach (var rule in _tournament.Leaderboard.Rules)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Rules.Add(new LeaderboardRuleViewModel(rule));
            });
        }
    }

    private void AddEntry(LeaderboardEntryViewModel entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Add(entry);
            _tournamentViewModel.PresetIsModified();
        });
    }
    private void RemoveEntry(LeaderboardEntryViewModel entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Remove(entry);
            _tournamentViewModel.PresetIsModified();
        });
    }

    public void AddLeaderboardEntry(LeaderboardEntry entry, Player player)
    {
        _tournament.Leaderboard.Entries.Add(entry);
        AddEntry(new LeaderboardEntryViewModel(entry, player));
    }
    public void RemoveLeaderboardEntry(LeaderboardEntryViewModel entry)
    {
        _tournament.Leaderboard.Entries.Remove(entry.GetLeaderboardEntry());
        RemoveEntry(entry);
    }

    public void AddRule(LeaderboardRule rule)
    {
        var ruleViewModel = new LeaderboardRuleViewModel(rule);
        _tournament.Leaderboard.Rules.Add(rule);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Add(ruleViewModel);
            _tournamentViewModel.PresetIsModified();
        });
    }
    public void RemoveRule(LeaderboardRuleViewModel rule)
    {
        _tournament.Leaderboard.Rules.Remove(rule.GetLeaderboardRule());
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Remove(rule);
            _tournamentViewModel.PresetIsModified();
        });
    }
    
    public void RefreshUI()
    {
        OnPropertyChanged(nameof(Entries));
        OnPropertyChanged(nameof(Rules));
    }

    public void Clear()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Entries.Clear();
            _tournamentViewModel.PresetIsModified();
        });
    }
}