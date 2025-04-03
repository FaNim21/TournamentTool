using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
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
    public ICommand AddRuleCommand { get; set; }

    public ICommand EditRuleCommand { get; set; }
    public ICommand RemoveRuleCommand { get; set; }

    public ICommand EditEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }
    

    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetSaver) : base(coordinator)
    {
        Tournament = tournament;
        PresetSaver = presetSaver;

        Leaderboard = Tournament.Leaderboard;
        
        AddEntryCommand = new RelayCommand( () => Leaderboard.AddLeaderboardEntry(new LeaderboardEntry(), null!));
        AddRuleCommand = new RelayCommand(() => Leaderboard.AddRule(new LeaderboardRule()));

        EditRuleCommand = new RelayCommand(null);
        RemoveRuleCommand = new RemoveRuleCommand(this);
        
        EditEntryCommand = new RelayCommand(null);
        RemoveEntryCommand = new RemoveEntryCommand(this);
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    }
    public override void OnEnable(object? parameter)
    {
        Leaderboard = Tournament.Leaderboard;
    }
    public override bool OnDisable()
    {
        PresetSaver.SavePreset();
        return true;
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