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
        AddEntry("fanim21");
    }
    public override bool OnDisable()
    {
        return true;
    }

    public void AddEntry(string ign)
    {
        if (string.IsNullOrEmpty(ign)) return;
        
       var player = Tournament.GetPlayerByIGN(ign);
       if (player == null) return;
       if (IsDuplicated(ign)) return;
       
        LeaderboardEntry entry = new()
        {
            PlayerUUID = player.UUID,
        };
       
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