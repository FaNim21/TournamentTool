using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Exceptions;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.Leaderboard;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Ranking;

namespace TournamentTool.ViewModels.Selectable;

public class LeaderboardPanelViewModel : SelectableViewModel
{
    private readonly ITournamentLeaderboardRepository _leaderboardRepository;
    private readonly ITournamentState _tournamentState;
    private readonly ILuaScriptsManager _luaScriptsManager;
    private readonly IDialogService _dialogService;
    private readonly ITournamentPlayerRepository _playerRepository;
    private ILeaderboardManager LeaderboardManager { get; }


    public ObservableCollection<LeaderboardEntryViewModel> Entries { get; } = [];
    private int _entriesViewRefreshTrigger = 0;
    public int EntriesViewRefreshTrigger
    {
        get => _entriesViewRefreshTrigger;
        set
        {
            _entriesViewRefreshTrigger = value;
            OnPropertyChanged(nameof(EntriesViewRefreshTrigger));
        }
    }

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


    public LeaderboardPanelViewModel(ICoordinator coordinator, ITournamentLeaderboardRepository leaderboardRepository, ITournamentState tournamentState,
        ILeaderboardManager leaderboardManager, ILuaScriptsManager luaScriptsManager, IWindowService windowService, IDispatcherService dispatcher, 
        IDialogService dialogService, ITournamentPlayerRepository playerRepository) : base(coordinator, dispatcher)
    {
        LeaderboardManager = leaderboardManager;
        _leaderboardRepository = leaderboardRepository;
        _tournamentState = tournamentState;
        _luaScriptsManager = luaScriptsManager;
        _dialogService = dialogService;
        _playerRepository = playerRepository;

        AddRuleCommand = new RelayCommand(() => AddRule(new LeaderboardRule()));
        AddFromWhitelistCommand = new RelayCommand(() => { windowService.ShowLoading(AddFromWhitelist); });

        EditRuleCommand = new EditRuleCommand(windowService, tournamentState, luaScriptsManager, dialogService, dispatcher);
        RemoveRuleCommand = new RemoveRuleCommand(this, dialogService);

        ViewEntryCommand = new ViewEntryCommand(windowService);
        EditEntryCommand = new EditEntryCommand(windowService, tournamentState, this, dispatcher, dialogService);
        RemoveEntryCommand = new RemoveEntryCommand(this, dialogService);
        RemoveAllEntriesCommand = new RelayCommand(RemoveAllEntries);

        OpenScriptingDocsCommand = new RelayCommand(() => { Helper.StartProcess("https://github.com/FaNim21/TournamentTool/blob/master/Docs/LeaderboardScripting.md");});
        RefreshScriptsCommand = new RelayCommand(RefreshScripts);
        OpenScriptsFolderCommand = new RelayCommand(() => { Helper.StartProcess(Consts.ScriptsPath);});

        MoveRuleItemCommand = new RelayCommand<(int oldIndex, int newIndex)>(MoveRuleItem);
    }
    
    public override bool CanEnable()
    {
        return _tournamentState.IsCurrentlyOpened;
    }

    public override void OnEnable(object? parameter)
    {
        Setup();
        LeaderboardManager.OnEntryUpdate += OnEntryUpdate;
    }
    public override bool OnDisable()
    {
        LeaderboardManager.OnEntryUpdate -= OnEntryUpdate;

        Dispatcher.Invoke(() =>
        {
            Rules.Clear();
            Entries.Clear();
        });
        return true;
    }
    
    private void Setup()
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var entry in _leaderboardRepository.OrderedEntries)
            {
                var player = _playerRepository.GetPlayerByUUID(entry.PlayerUUID);
                if (player is not PlayerViewModel playerViewModel) continue;
                
                Entries.Add(new LeaderboardEntryViewModel(entry, playerViewModel, Dispatcher));
            }
        
            foreach (var rule in _leaderboardRepository.Rules)
            {
                Rules.Add(new LeaderboardRuleViewModel(rule, _tournamentState, _dialogService, Dispatcher));
            }

            if (Rules.Count > 1)
                Rules[0].IsFocused = true;
        });
        
        /*var collectionViewSource = CollectionViewSource.GetDefaultView(Entries);
        using (collectionViewSource.DeferRefresh())
        {
            collectionViewSource.Filter = null;
            collectionViewSource.SortDescriptions.Clear();
            collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(LeaderboardEntryViewModel.Position), ListSortDirection.Ascending));
        }*/
        
        RefreshAllEntries();
    }

    private void OnEntryUpdate(LeaderboardEntry newEntry)
    {
        var entryViewModel = Entries.FirstOrDefault(vm => vm.GetLeaderboardEntry().PlayerUUID.Equals(newEntry.PlayerUUID));
        if (entryViewModel == null)
        {
            var player = _playerRepository.GetPlayerByUUID(newEntry.PlayerUUID);
            if (player is not PlayerViewModel playerViewModel) return;
            
            entryViewModel = new LeaderboardEntryViewModel(newEntry, playerViewModel, Dispatcher);
            Dispatcher.Invoke(() =>
            {
                Entries.Add(entryViewModel);
            });
        }
        else
        {
            RefreshEntriesPanel();
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
            _leaderboardRepository.RecalculateEntryPosition(entry.GetLeaderboardEntry());
            entry.Refresh(Rules[0].ChosenMilestone);
        }

        RefreshEntriesPanel();
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
        _leaderboardRepository.AddEntry(entry);
        Dispatcher.Invoke(() =>
        {
            Entries.Add(new LeaderboardEntryViewModel(entry, playerViewModel, Dispatcher));
            _tournamentState.MarkAsModified();
        });
    }
    public void RemoveLeaderboardEntry(LeaderboardEntryViewModel entry)
    {
        _leaderboardRepository.RemoveEntry(entry.GetLeaderboardEntry());
        Dispatcher.Invoke(() =>
        {
            Entries.Remove(entry);
            _tournamentState.MarkAsModified();
        });
    }

    public void AddRule(LeaderboardRule rule)
    {
        var ruleViewModel = new LeaderboardRuleViewModel(rule, _tournamentState, _dialogService, Dispatcher);
        _leaderboardRepository.AddRule(rule);
        Dispatcher.Invoke(() =>
        {
            Rules.Add(ruleViewModel);
            _tournamentState.MarkAsModified();
        });
        UpdateFocusedRule();
    }
    public void RemoveRule(LeaderboardRuleViewModel rule)
    {
        _leaderboardRepository.RemoveRule(rule.GetLeaderboardRule());
        Dispatcher.Invoke(() =>
        {
            Rules.Remove(rule);
            _tournamentState.MarkAsModified();
        });
        UpdateFocusedRule();
    }

    private void RefreshEntriesPanel()
    {
        EntriesViewRefreshTrigger++;
    }
    
    private void MoveRuleItem((int oldIndex, int newIndex) indexTuple)
    {
        int oldIndex = indexTuple.oldIndex;
        int newIndex = indexTuple.newIndex;
        
        if (oldIndex < 0 || 
            newIndex < 0 || 
            oldIndex == newIndex || 
            oldIndex >= _leaderboardRepository.Rules.Count ||
            newIndex >= _leaderboardRepository.Rules.Count) return;
        
        Rules.Move(oldIndex, newIndex);
        _leaderboardRepository.MoveRule(oldIndex, newIndex);
        
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
        int count = _playerRepository.Players.Count;
        for (var i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            progress.Report((float)i / count);
            
            var player = _playerRepository.Players[i];
            
            if (string.IsNullOrEmpty(player.UUID)) continue;
            if (_leaderboardRepository.GetEntry(player.UUID) != null) continue;
            
            logProgress.Report($"({i+1})/{count}) Adding player {player.Name} to leaderboard");
            
            var entry = new LeaderboardEntry { PlayerUUID = player.UUID };
            AddLeaderboardEntry(entry, (PlayerViewModel)player);

            await Task.Delay(10);
        }
        
        logProgress.Report($"Recalculating all positions");
        RecalculateAllEntries();
        await Task.Delay(25);
    }
    
    private void RemoveAllEntries()
    {
        if (_dialogService.Show($"Are you sure you want to remove all entries in leaderboard?",
                "Removing all leaderboard entries", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        
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

            try
            {
                _luaScriptsManager.AddOrReload(name);
            }
            catch (LuaScriptValidationException ex)
            {
                _dialogService.Show(ex.ToString(), $"Script Validation ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}