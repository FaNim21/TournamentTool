using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class LeaderboardPanelViewModel : SelectableViewModel
{
    public IPresetSaver PresetSaver { get; private set; }

    private TournamentViewModel Tournament { get; }
    private Leaderboard Leaderboard { get; }

    public ObservableCollection<LeaderboardEntryViewModel> Entries { get; } = [];
    public ObservableCollection<LeaderboardRuleViewModel> Rules { get; } = [];

    public ICommand AddEntryCommand { get; set; }
    public ICommand AddRuleCommand { get; set; }

    public ICommand EditRuleCommand { get; set; }
    public ICommand RemoveRuleCommand { get; set; }

    public ICommand ViewEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }
    
    public ICommand RefreshScriptsCommand { get; set; }
    public ICommand OpenScriptsFolderCommand { get; set; }
    

    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetSaver) : base(coordinator)
    {
        Tournament = tournament;
        PresetSaver = presetSaver;

        Leaderboard = Tournament.Leaderboard;
        
        AddEntryCommand = new RelayCommand( () => AddLeaderboardEntry(new LeaderboardEntry(), null!));
        AddRuleCommand = new RelayCommand(() => AddRule(new LeaderboardRule()));

        EditRuleCommand = new EditRuleCommand(coordinator, Tournament);
        RemoveRuleCommand = new RemoveRuleCommand(this);

        ViewEntryCommand = new ViewEntryCommand(coordinator);
        RemoveEntryCommand = new RemoveEntryCommand(this);

        RefreshScriptsCommand = new RelayCommand(RefreshScripts);
        OpenScriptsFolderCommand = new RelayCommand(() => { Helper.StartProcess(Consts.ScriptsPath);});
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    }

    public override void OnEnable(object? parameter)
    {
        Setup();
    }
    public override bool OnDisable()
    {
        PresetSaver.SavePreset();
        return true;
    }
    
    private void Setup()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var entry in Leaderboard.Entries)
            {
                var player = Tournament.GetPlayerByUUID(entry.PlayerUUID);
                Entries.Add(new LeaderboardEntryViewModel(entry, player));
            }
        
            foreach (var rule in Leaderboard.Rules)
            {
                Rules.Add(new LeaderboardRuleViewModel(rule));
            }
        });
    }
    
    private void AddEntry(LeaderboardEntryViewModel entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Add(entry);
            Tournament.PresetIsModified();
        });
    }
    private void RemoveEntry(LeaderboardEntryViewModel entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Remove(entry);
            Tournament.PresetIsModified();
        });
    }

    public void AddLeaderboardEntry(LeaderboardEntry entry, PlayerViewModel playerViewModel)
    {
        Tournament.Leaderboard.Entries.Add(entry);
        AddEntry(new LeaderboardEntryViewModel(entry, playerViewModel));
    }
    public void RemoveLeaderboardEntry(LeaderboardEntryViewModel entry)
    {
        Tournament.Leaderboard.Entries.Remove(entry.GetLeaderboardEntry());
        RemoveEntry(entry);
    }

    public void AddRule(LeaderboardRule rule)
    {
        var ruleViewModel = new LeaderboardRuleViewModel(rule);
        Tournament.Leaderboard.Rules.Add(rule);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Add(ruleViewModel);
            Tournament.PresetIsModified();
        });
    }
    public void RemoveRule(LeaderboardRuleViewModel rule)
    {
        Tournament.Leaderboard.Rules.Remove(rule.GetLeaderboardRule());
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Remove(rule);
            Tournament.PresetIsModified();
        });
    }

    private void RefreshScripts()
    {
        
    }
}