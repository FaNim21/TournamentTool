using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MethodTimer;
using MoonSharp.Interpreter;
using TournamentTool.Commands;
using TournamentTool.Commands.Leaderboard;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.Lua;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Ranking;

namespace TournamentTool.ViewModels.Selectable;

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

    public ICommand AddRuleCommand { get; private set; }
    public ICommand AddFromWhitelistCommand { get; private set; }

    public ICommand EditRuleCommand { get; set; }
    public ICommand RemoveRuleCommand { get; set; }

    public ICommand ViewEntryCommand { get; set; }
    public ICommand EditEntryCommand { get; set; }
    public ICommand RemoveEntryCommand { get; set; }
    public ICommand RemoveAllEntriesCommand { get; set; }

    public ICommand OpenScriptingDocsCommand { get; private set; }
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
        AddFromWhitelistCommand = new RelayCommand(() => { Coordinator.ShowLoading(AddFromWhitelist); });

        EditRuleCommand = new EditRuleCommand(coordinator, Tournament, luaScriptsManager);
        RemoveRuleCommand = new RemoveRuleCommand(this);

        ViewEntryCommand = new ViewEntryCommand(coordinator);
        EditEntryCommand = new EditEntryCommand(coordinator, tournament, this);
        RemoveEntryCommand = new RemoveEntryCommand(this);
        RemoveAllEntriesCommand = new RelayCommand(RemoveAllEntries);

        OpenScriptingDocsCommand = new RelayCommand(() => { Helper.StartProcess("https://github.com/FaNim21/TournamentTool/blob/master/Docs/LeaderboardScripting.md");});
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

        Application.Current.Dispatcher.Invoke(() =>
        {
            EntriesCollection = null;
            Rules.Clear();
            Entries.Clear();
        });
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
                Rules.Add(new LeaderboardRuleViewModel(rule, Tournament));
            }

            if (Rules.Count > 1)
                Rules[0].IsFocused = true;
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

        for (int i = 0; i < Rules.Count; i++)
        {
            LeaderboardRuleViewModel rule = Rules[i];
            for (int j = 0; j < rule.SubRules.Count; j++)
            {
                LeaderboardSubRuleViewModel subRule = rule.SubRules[j];
                subRule.UpdateCustomVariablesValues();
            }
        }
        
        RefreshAllEntries();
    }

    public void RecalculateAllEntries()
    {
        if (Rules.Count == 0) return;
            
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
        if (Rules.Count == 0) return;
        
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
        var ruleViewModel = new LeaderboardRuleViewModel(rule, Tournament);
        Leaderboard.AddRule(rule);
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Add(ruleViewModel);
            Tournament.PresetIsModified();
        });
        UpdateFocusedRule();
    }
    public void RemoveRule(LeaderboardRuleViewModel rule)
    {
        Leaderboard.RemoveRule(rule.GetLeaderboardRule());
        Application.Current.Dispatcher.Invoke(() =>
        {
            Rules.Remove(rule);
            Tournament.PresetIsModified();
        });
        UpdateFocusedRule();
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
        UpdateFocusedRule();
    }

    private void UpdateFocusedRule()
    {
        if (Rules.Count == 0) return;
        
        for (int i = 0; i < Rules.Count; i++)
        {
            var current = Rules[i];
            current.IsFocused = false;
        }
        Rules[0].IsFocused = true;
    }

    private async Task AddFromWhitelist(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        int count = Tournament.Players.Count;
        for (var i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            progress.Report((float)i / count);
            
            var player = Tournament.Players[i];
            if (string.IsNullOrEmpty(player.UUID)) continue;
            if (Leaderboard.GetEntry(player.UUID) != null) continue;
            
            logProgress.Report($"({i+1})/{count}) Adding player {player.DisplayName} to leaderboard");
            
            var entry = new LeaderboardEntry { PlayerUUID = player.UUID };
            AddLeaderboardEntry(entry, player);

            await Task.Delay(10);
        }
        
        logProgress.Report($"Recalculating all positions");
        RecalculateAllEntries();
        await Task.Delay(25);
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

    // TO ODSWIEZANIE Z WALIDACJA SA POKI NIE BEDZIE ODDZIELNEGO MIEJSCA DO SPRAWDZANIA SKRYPTOW
    private void RefreshScripts()
    {
        var scripts = Directory.GetFiles(Consts.LeaderboardScriptsPath, "*.lua", SearchOption.TopDirectoryOnly).AsSpan();
        for (int i = 0; i < scripts.Length; i++)
        {
            var script = scripts[i];
            var name = Path.GetFileNameWithoutExtension(script);

            string output = ValidateScript(name);
            if (!string.IsNullOrWhiteSpace(output))
            {
                string finalOutput = $"Error in: {name}.lua\n" + output;
                DialogBox.Show(finalOutput, $"Script Validation ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                break;
            }

            try
            {
                _luaScriptsManager.AddOrReload(name);
            }
            catch{ /**/ }
        }
    }
    public string ValidateScript(string scriptName)
    {
        var scriptPath = Path.Combine(Consts.LeaderboardScriptsPath, $"{scriptName}.lua");
        var expectedType = Tournament.ControllerMode == ControllerMode.Ranked ? LuaLeaderboardType.ranked : LuaLeaderboardType.normal;
        var result = LuaScriptValidator.ValidateScriptWithRuntime(scriptPath, expectedType);

        if (result.IsValid) return string.Empty;
        
        var errors = new List<string>();
        if (result.SyntaxError != null)
            errors.Add($"Syntax: {result.SyntaxError.Message}");
                    
        errors.AddRange(result.RuntimeErrors.Select(e => $"Runtime: {e.Message}"));
        return string.Join("\n", errors);
    }
}