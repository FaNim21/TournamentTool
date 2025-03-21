﻿using System.Collections.ObjectModel;
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
    
    
    public LeaderboardViewModel(Tournament tournament, TournamentViewModel tournamentViewModel)
    {
        _tournament = tournament;
        _tournamentViewModel = tournamentViewModel;
    }

    public void Update(TournamentViewModel tournamentViewModel)
    {
        Entries.Clear();
        foreach (var entry in _tournament.Leaderboard.Entries)
        {
            var player = tournamentViewModel.GetPlayerByUUID(entry.PlayerUUID);
            if (player == null) continue;
            
            Entries.Add(new LeaderboardEntryViewModel(entry, player));
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
        _tournament.Leaderboard.Entries.Add(entry.GetLeaderboardEntry());
        RemoveEntry(entry);
    }
    
    public void RefreshUI()
    {
        OnPropertyChanged(nameof(Entries));
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