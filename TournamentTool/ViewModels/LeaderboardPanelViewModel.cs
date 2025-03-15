using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public sealed class LeaderboardPanelViewModel : SelectableViewModel
{
    public IPresetSaver PresetSaver { get; private set; }

    public TournamentViewModel Tournament { get; }
    public LeaderboardViewModel Leaderboard { get; set; }

    public ICommand AddEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }


    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetSaver) : base(coordinator)
    {
        Tournament = tournament;
        PresetSaver = presetSaver;
        
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
        AddEntry("ec20efb08e3741c3898014696cbb2748");
    }
    public override bool OnDisable()
    {
        return true;
    }

    public void AddEntry(string uuid)
    {
        if (string.IsNullOrEmpty(uuid)) return;
        if (IsDuplicated(uuid)) return;
        
        //to wiadomo nie skonczone jako idea pod mozliwosc dobrze przekminionej logiki dodawania danych do leaderboard'a
        LeaderboardEntry entry = new()
        {
            PlayerUUID = uuid,
        };
        
       var player = Tournament.GetPlayerByGUID(uuid);
       if (player == null) return;
       
       Leaderboard.AddLeaderboardEntry(entry, player);
    }

    public void RemoveEntry(LeaderboardEntryViewModel entry)
    {
        //...
        Leaderboard.RemoveLeaderboardEntry(entry);
    }

    private bool IsDuplicated(string uuid)
    {
        foreach (var entry in Leaderboard.Entries)
        {
            if (!entry.GetLeaderboardEntry().PlayerUUID.Equals(uuid)) continue;
            return true;
        }
        
        return false;
    }
}