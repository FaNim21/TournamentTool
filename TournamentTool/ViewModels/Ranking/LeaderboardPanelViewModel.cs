using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MethodTimer;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Components.Controls;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Ranking;

public class LeaderboardPanelViewModel : SelectableViewModel
{
    private readonly ILuaScriptsManager _luaScriptsManager;
    
    public IPresetSaver PresetSaver { get; private set; }
    private ILeaderboardManager LeaderboardManager { get; }

    private TournamentViewModel Tournament { get; }
    private Leaderboard Leaderboard { get; }

    public ICollectionView? EntriesCollection { get; set; }
    public ObservableCollection<LeaderboardEntryViewModel> Entries { get; } = [];
    public ObservableCollection<LeaderboardRuleViewModel> Rules { get; } = [];

    public ICommand AddRuleCommand { get; set; }

    public ICommand EditRuleCommand { get; set; }
    public ICommand RemoveRuleCommand { get; set; }

    public ICommand ViewEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }
    public ICommand RemoveAllEntriesCommand { get; set; }

    public ICommand RefreshScriptsCommand { get; set; }
    public ICommand OpenScriptsFolderCommand { get; set; }

    public ICommand MoveRuleItemCommand { get; set; }


    public LeaderboardPanelViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetSaver, ILeaderboardManager leaderboardManager, ILuaScriptsManager luaScriptsManager) : base(coordinator)
    {
        Tournament = tournament;
        PresetSaver = presetSaver;
        LeaderboardManager = leaderboardManager;
        _luaScriptsManager = luaScriptsManager;

        Leaderboard = Tournament.Leaderboard; 
        
        AddRuleCommand = new RelayCommand(() => AddRule(new LeaderboardRule()));

        EditRuleCommand = new EditRuleCommand(coordinator, Tournament, luaScriptsManager);
        RemoveRuleCommand = new RemoveRuleCommand(this);

        ViewEntryCommand = new ViewEntryCommand(coordinator);
        RemoveEntryCommand = new RemoveEntryCommand(this);
        RemoveAllEntriesCommand = new RelayCommand(RemoveAllEntries);

        RefreshScriptsCommand = new RelayCommand(RefreshScripts);
        OpenScriptsFolderCommand = new RelayCommand(() => { Helper.StartProcess(Consts.ScriptsPath);});

        MoveRuleItemCommand = new RelayCommand<(int oldIndex, int newIndex)>(MoveRuleItem);
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    }

    public override void OnEnable(object? parameter)
    {
        Setup();
        LeaderboardManager.OnEntryUpdate += OnEntryUpdate;
    }
    public override bool OnDisable()
    {
        LeaderboardManager.OnEntryUpdate -= OnEntryUpdate;
        
        PresetSaver.SavePreset();
        return true;
    }
    
    private void Setup()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var entry in Leaderboard.OrderedEntries)
            {
                var player = Tournament.GetPlayerByUUID(entry.PlayerUUID);
                Entries.Add(new LeaderboardEntryViewModel(entry, player));
            }
        
            foreach (var rule in Leaderboard.Rules)
            {
                Rules.Add(new LeaderboardRuleViewModel(rule));
            }
        });
        
        var collectionViewSource = CollectionViewSource.GetDefaultView(Entries);
        using (collectionViewSource.DeferRefresh())
        {
            collectionViewSource.Filter = null;
            collectionViewSource.SortDescriptions.Clear();
            collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(LeaderboardEntryViewModel.Position), ListSortDirection.Ascending));
        }
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            EntriesCollection = collectionViewSource;
        });
        
        RefreshAllEntries();
    }

    private void OnEntryUpdate(LeaderboardEntry newEntry)
    {
        var entryViewModel = Entries.FirstOrDefault(vm => vm.GetLeaderboardEntry().PlayerUUID.Equals(newEntry.PlayerUUID));
        if (entryViewModel == null)
        {
            var player = Tournament.GetPlayerByUUID(newEntry.PlayerUUID);
            entryViewModel = new LeaderboardEntryViewModel(newEntry, player);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Entries.Add(entryViewModel);
            });
        }
        else
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                EntriesCollection?.Refresh();
            });
        }
        
        RefreshAllEntries();
    }

    private void RecalculateAllEntries()
    {
        foreach (var entry in Entries)
        {
            Leaderboard.RecalculateEntryPosition(entry.GetLeaderboardEntry());
            entry.Refresh(Rules[0].ChosenMilestone);
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            EntriesCollection?.Refresh();
        });
    }
    public void RefreshAllEntries()
    {
        foreach (var entry in Entries)
        {
            entry.Refresh(Rules[0].ChosenMilestone);
        }
    }
    
    public void AddLeaderboardEntry(LeaderboardEntry entry, PlayerViewModel playerViewModel)
    {
        Leaderboard.AddEntry(entry);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Add(new LeaderboardEntryViewModel(entry, playerViewModel));
            Tournament.PresetIsModified();
        });
    }
    public void RemoveLeaderboardEntry(LeaderboardEntryViewModel entry)
    {
        Leaderboard.RemoveEntry(entry.GetLeaderboardEntry());
        Application.Current.Dispatcher.Invoke(() =>
        {
            Entries.Remove(entry);
            Tournament.PresetIsModified();
        });
    }

    public void AddRule(LeaderboardRule rule)
    {
        var ruleViewModel = new LeaderboardRuleViewModel(rule);
        Leaderboard.AddRule(rule);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Add(ruleViewModel);
            Tournament.PresetIsModified();
        });
    }
    public void RemoveRule(LeaderboardRuleViewModel rule)
    {
        Leaderboard.RemoveRule(rule.GetLeaderboardRule());
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Remove(rule);
            Tournament.PresetIsModified();
        });
    }

    private void MoveRuleItem((int oldIndex, int newIndex) indexTuple)
    {
        int oldIndex = indexTuple.oldIndex;
        int newIndex = indexTuple.newIndex;
        
        if (oldIndex < 0 || 
            newIndex < 0 || 
            oldIndex == newIndex || 
            oldIndex >= Leaderboard.Rules.Count ||
            newIndex >= Leaderboard.Rules.Count) return;
        
        Rules.Move(oldIndex, newIndex);
        Leaderboard.MoveRule(oldIndex, newIndex);
        
        RecalculateAllEntries();
    }
    
    private void RemoveAllEntries()
    {
        if (DialogBox.Show($"Are you sure you want to remove all entries in leaderboard?",
                "Removing all leaderboard entries", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
            MessageBoxResult.Yes) return;
        
        foreach (var entry in Entries.ToList())
        {
            RemoveLeaderboardEntry(entry);
        }
    }

    private void RefreshScripts()
    {
        _luaScriptsManager.LoadLuaScripts();
        
        //TODO: 0 Zrobic kompilacje skryptow pod wykrywanie w nich bledow
        //TODO: 0 Przy okazji tez dodac nowa zmienna w skryptach czyli tryb jak: Ranked, Normal i od razu z pobierania listy skryptow trzeba zwracac te z obecnie wybranego skryptu
        foreach (var script in _luaScriptsManager.GetScriptsList())
        {
            
        }
    }
}