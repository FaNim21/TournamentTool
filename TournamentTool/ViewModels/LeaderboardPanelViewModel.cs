using System.Windows.Input;
using Microsoft.Xaml.Behaviors.Core;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public sealed class LeaderboardPanelViewModel : SelectableViewModel
{
    public TournamentViewModel Tournament { get; }
    public LeaderboardViewModel Leaderboard { get; set; }

    public ICommand AddEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }


    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament) : base(coordinator)
    {
        Tournament = tournament;
        Leaderboard = tournament.Leaderboard;

        AddEntryCommand = new RelayCommand(null);
        RemoveEntryCommand = new RelayCommand(null);
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    }
    public override void OnEnable(object? parameter)
    {
        
    }
    public override bool OnDisable()
    {
        return true;
    }

    public void AddEntry(Guid playerID)
    {
        //to wiadomo nie skonczone jako idea pod mozliwosc dobrze przekminionej logiki dodawania danych do leaderboard'a
        LeaderboardEntry entry = new()
        {
            PlayerID = playerID,
        };
        
       var player = Tournament.GetPlayerByGUID(playerID);
       if (player == null) return;
       
       Leaderboard.AddLeaderboardEntry(entry, player);
    }

    public void RemoveEntry(LeaderboardEntryViewModel entry)
    {
        //...
        Leaderboard.RemoveLeaderboardEntry(entry);
    }
}