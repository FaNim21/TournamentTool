using System.IO;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
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

    public ICommand ViewEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }
    

    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetSaver) : base(coordinator)
    {
        Tournament = tournament;
        PresetSaver = presetSaver;

        Leaderboard = Tournament.Leaderboard;
        
        AddEntryCommand = new RelayCommand( () => Leaderboard.AddLeaderboardEntry(new LeaderboardEntry(), null!));
        AddRuleCommand = new RelayCommand(() => Leaderboard.AddRule(new LeaderboardRule()));

        EditRuleCommand = new EditRuleCommand(coordinator);
        RemoveRuleCommand = new RemoveRuleCommand(this);

        ViewEntryCommand = new ViewEntryCommand(coordinator);
        RemoveEntryCommand = new RemoveEntryCommand(this);
        
        if (!Directory.Exists(Consts.AppAPIPath))
            Directory.CreateDirectory(Consts.AppAPIPath);
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

    public void EvaluatePlayer(Player player)
    {
        if (Leaderboard.Rules.Count == 0) return;
        
        foreach (var rule in Leaderboard.Rules)
        {
            rule.Evaluate();
        }
    }

    public void EvaluatePlayers(List<Player> players)
    {
        if (Leaderboard.Rules.Count == 0) return;
        
        foreach (var rule in Leaderboard.Rules)
        {
            rule.Evaluate();
        }
    }
}