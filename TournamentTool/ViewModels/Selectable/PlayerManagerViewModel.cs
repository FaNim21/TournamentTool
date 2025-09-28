using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
using TournamentTool.Components.Controls;
using TournamentTool.Enums;
using TournamentTool.Extensions;
using TournamentTool.Factories;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TournamentTool.Services.Background;
using TournamentTool.Services.External;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels.Selectable;

public class PlayerManagerViewModel : SelectableViewModel, IPlayerManager, IPlayerAddReceiver
{
    public ObservableCollection<PaceManEvent> PaceManEvents { get; set; } = [];
     
    public TournamentViewModel Tournament { get; }
    public IPresetSaver PresetService { get; }
    private IBackgroundCoordinator BackgroundCoordinator { get; }
    public ILoggingService Logger { get; }
    public ISettings SettingsService { get; }
    public IPlayerViewModelFactory PlayerViewModelFactory { get; }

    private ObservableCollection<PlayerViewModel> _selectedPlayers = [];
    public ObservableCollection<PlayerViewModel> SelectedPlayers
    {
        get => _selectedPlayers;
        set
        {
            _selectedPlayers = value;
            OnPropertyChanged(nameof(SelectedPlayers));
        }
    }
    
    public ICollectionView? FilteredPlayersCollectionView { get; set; }
    
    private PaceManEvent? _chosenEvent = new();
    public PaceManEvent? ChosenEvent
    {
        get => _chosenEvent;
        set
        {
            _chosenEvent = value;
            OnPropertyChanged(nameof(ChosenEvent));
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));

            IsSearchEmpty = string.IsNullOrEmpty(SearchText);
        }
    }
    
    private PlayerSortingType _sortingType = PlayerSortingType.Name;
    public PlayerSortingType SortingType
    {
        get => _sortingType;
        set
        {
            if (value == _sortingType) return;
            _sortingType = value;
            OnPropertyChanged(nameof(SortingType));
        }
    }

    private string _informationCount = string.Empty;
    public string InformationCount
    {
        get => _informationCount;
        set
        {
            _informationCount = value;
            OnPropertyChanged(nameof(InformationCount));
        }
    }

    private bool _isSearchEnabled = true;
    public bool IsSearchEnabled
    {
        get => _isSearchEnabled;
        set
        {
            _isSearchEnabled = value;
            OnPropertyChanged(nameof(IsSearchEnabled));
        }
    }
    
    private bool _isSearchEmpty = true;
    public bool IsSearchEmpty
    {
        get => _isSearchEmpty;
        set
        {
            if (IsSearchEmpty == value) return;
            _isSearchEmpty = value;
            OnPropertyChanged(nameof(IsSearchEmpty));
        }
    }

    private bool _showPlayers;
    public bool ShowPlayers
    {
        get => _showPlayers;
        set
        {
            _showPlayers = value;
            OnPropertyChanged(nameof(ShowPlayers));
        }
    }

    public ICommand AddPlayerCommand { get; set; }
    public ICommand ValidatePlayersCommand { get; set; }

    public ICommand ViewPlayerInfoCommand { get; set; }
    public ICommand EditPlayerCommand { get; set; }
    public ICommand ValidatePlayerCommand { get; set; }
    public ICommand FixPlayerHeadCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }
    
    public ICommand RemoveSelectedPlayerCommand { get; set; }

    public ICommand ImportPlayersCommand { get; set; }
    public ICommand ExportPlayersCommand { get; set; }

    public ICommand LoadFromPaceManCommand { get; set; }

    public ICommand RemoveAllPlayerCommand { get; set; }
    public ICommand FixPlayersHeadsCommand { get; set; }
    
    public ICommand SubmitSearchCommand { get; set; }
    public ICommand ClearSearchFieldCommand { get; set; }

    private const StringComparison _comparison = StringComparison.OrdinalIgnoreCase;
    private string _lastFilterSearch = "filter";
    private PlayerSortingType _lastSortingType;


    public PlayerManagerViewModel(ICoordinator coordinator, TournamentViewModel tournament, IPresetSaver presetService, IBackgroundCoordinator backgroundCoordinator, ILoggingService logger, ISettings settingsService, IPlayerViewModelFactory playerViewModelFactory, IPacemanAPIService pacemanApiService) : base(coordinator)
    {
        Tournament = tournament;
        PresetService = presetService;
        BackgroundCoordinator = backgroundCoordinator;
        Logger = logger;
        SettingsService = settingsService;
        PlayerViewModelFactory = playerViewModelFactory;

        AddPlayerCommand = new RelayCommand(AddPlayer);
        ValidatePlayersCommand = new RelayCommand(() => { Coordinator.ShowLoading(ValidateAllPlayers); });

        ViewPlayerInfoCommand = new ViewPlayerInfoCommand(this, coordinator, coordinator, PresetService, Logger);
        EditPlayerCommand = new EditPlayerCommand(this);
        ValidatePlayerCommand = new ValidatePlayerCommand(this, coordinator, PresetService);
        FixPlayerHeadCommand = new FixPlayerHeadCommand(this, coordinator, PresetService);
        RemovePlayerCommand = new RemovePlayerCommand(this, PresetService);

        RemoveSelectedPlayerCommand = new RelayCommand(RemoveSelectedPlayer);

        ImportPlayersCommand = new ImportWhitelistCommand(this, Tournament, coordinator, PresetService, Logger);
        ExportPlayersCommand = new ExportWhitelistCommand(Tournament, tournament.GetData());

        LoadFromPaceManCommand = new LoadDataFromPacemanCommand(this, Tournament, PresetService, coordinator, Logger);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand( () => { Coordinator.ShowLoading(FixPlayersHeads); });

        SubmitSearchCommand = new RelayCommand(()=> { FilterWhitelist(); });
        ClearSearchFieldCommand = new RelayCommand(ClearFilters);
        
        Task.Run(async () => 
        {
            PaceManEvent[]? eventsData = null;
            try
            {
                eventsData = await pacemanApiService.GetPacemanEvents();
            }
            catch (Exception ex)
            {
                Logger.Error($"Can't load paceman events: {ex.Message}"); 
            }

            if (eventsData == null) return;

            PaceManEvents = new ObservableCollection<PaceManEvent>(eventsData);
            OnPropertyChanged(nameof(PaceManEvents));
        });
    }

    public override bool CanEnable()
    {
        return !Tournament.IsNullOrEmpty();
    } 
    public override void OnEnable(object? parameter)
    {
        var collectionViewSource = CollectionViewSource.GetDefaultView(Tournament.Players);
        using (collectionViewSource.DeferRefresh())
        {
            collectionViewSource.Filter = null;
            collectionViewSource.Filter = FilterPlayers;
            collectionViewSource.SortDescriptions.Clear();
        }
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            FilteredPlayersCollectionView = collectionViewSource;
        });
        
        BackgroundCoordinator.Register(this);
        FilterWhitelist(true);
    }
    public override bool OnDisable()
    {
        BackgroundCoordinator.Unregister(this);
        ChosenEvent = null;
        ShowPlayers = false;
        
        return true;
    }

    private async Task ValidateAllPlayers(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
       int count = Tournament.Players.Count;
       for (int i = 0; i < count; i++)
       {
           var player = Tournament.Players[i];
           cancellationToken.ThrowIfCancellationRequested();
           progress.Report((float)i / count);
           if (string.IsNullOrEmpty(player.UUID)) continue;
           
           logProgress.Report($"({i+1}/{count}) validating player in game name: {player.InGameName}");
           await Task.Delay(500, cancellationToken);

           var data = await player.GetDataFromUUID();
           if (!data.HasValue) return;
        
           if (data.Value.InGameName.Equals(player.InGameName))
           {
               logProgress.Report($"({i+1}/{count}) player {player.InGameName} is correct");
           }
           else
           {
               logProgress.Report($"({i+1}/{count}) incorrect in game name... changing to: {player.InGameName}");
               player.InGameName = data.Value.InGameName;
           }
           await Task.Delay(500, cancellationToken);
       }
       PresetService.SavePreset();
    }

    public void Add(PlayerViewModel playerViewModel)
    {
        Tournament.AddPlayer(playerViewModel);
        
        UpdateInformationCountText();
        RefreshFilteredCollectionView();
    }
    public void Remove(PlayerViewModel playerViewModel)
    {
        Tournament.RemovePlayer(playerViewModel);
        UpdateInformationCountText();
    }
    
    private bool FilterPlayers(object obj)
    {
        if (obj is not PlayerViewModel player) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        return SortingType switch
        {
            PlayerSortingType.Name => player.Name?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.InGameName => player.InGameName?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.TeamName => player.TeamName?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.Stream =>
                (player.StreamData.Main?.Trim().Contains(SearchText, _comparison) ?? false) ||
                (player.StreamData.Alt?.Trim().Contains(SearchText, _comparison) ?? false) ||
                (player.StreamData.Other?.Trim().Contains(SearchText, _comparison) ?? false),
            _ => false
        };
    }
    
    public void FilterWhitelist(bool forceFilter = false)
    {
        if (!IsSearchEnabled) return;
        SearchText = SearchText.Trim();
        if (!forceFilter && _lastFilterSearch.Equals(SearchText, _comparison) && _lastSortingType.Equals(SortingType)) return;
        if (!forceFilter && string.IsNullOrEmpty(SearchText) && string.IsNullOrEmpty(_lastFilterSearch)) return;

        _lastFilterSearch = SearchText;
        _lastSortingType = SortingType;
        IsSearchEnabled = false;
        ShowPlayers = false;

        UpdateInformationCountText(string.IsNullOrEmpty(SearchText) ? "Restoring..." : "Filtering...", "?");
        RefreshFilteredCollectionView();
        UpdateInformationCountText();
        
        IsSearchEnabled = true;
        ShowPlayers = true;
    }

    private void RefreshFilteredCollectionView()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            FilteredPlayersCollectionView?.Refresh();
        }, DispatcherPriority.Background);
    }
    
    private void UpdateInformationCountText(string header = "Found", string filteredCount = "")
    {
        if (string.IsNullOrEmpty(filteredCount))
            filteredCount = FilteredPlayersCollectionView!.Cast<PlayerViewModel>().Count().ToString();
        
        InformationCount = $"{header} {filteredCount}/{Tournament.Players.Count}";
    }
    
    private void AddPlayer()
    {
        AddPlayer(null);
    }
    public void AddPlayer(Player? player)
    {
        PlayerViewModel? playerViewModel = null;
        if (player != null)
        {
            playerViewModel = PlayerViewModelFactory.Create(player);
        }
        
        WhitelistPlayerWindowViewModel viewModel = new(PlayerViewModelFactory, this, playerViewModel);
        WhitelistPlayerWindow window = new(viewModel);

        Coordinator.ShowDialog(window);
    }

    public async Task<bool> SavePlayer(PlayerViewModel playerViewModel, bool isEditing)
    {
        if (string.IsNullOrEmpty(playerViewModel.Name?.Trim()))
        {
            DialogBox.Show($"Name cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(playerViewModel.InGameName))
        {
            DialogBox.Show($"InGameName cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (isEditing)
        {
            if (!await EditPlayer(playerViewModel)) return false;
        }
        else
        {
            if (!await AddPlayerToWhitelist(playerViewModel)) return false;
        }

        return true;
    }
    private async Task<bool> EditPlayer(PlayerViewModel playerViewModel)
    {
        int n = Tournament!.Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Tournament!.Players[i];
            if (!current.Id.Equals(playerViewModel.Id)) continue;
            
            bool success = await UpdatePlayerData(current, playerViewModel, playerViewModel.Id);
            return success;
        }
        return false;
    }
    private async Task<bool> AddPlayerToWhitelist(PlayerViewModel playerViewModel)
    {
        PlayerViewModel newPlayerViewModel = PlayerViewModelFactory.Create();
        bool success = await UpdatePlayerData(newPlayerViewModel, playerViewModel);
        if (!success) return false;

        Add(newPlayerViewModel);
        return true;
    }
    private async Task<bool> UpdatePlayerData(PlayerViewModel playerViewModel, PlayerViewModel windowsData, Guid? excludeID = null)
    {
        if (Tournament.ContainsDuplicates(windowsData.Data, excludeID)) return false;

        playerViewModel.Name = windowsData.Name!;
        playerViewModel.InGameName = windowsData.InGameName!.Trim();
        playerViewModel.PersonalBest = windowsData.PersonalBest;
        playerViewModel.TeamName = windowsData.TeamName?.Trim();
        playerViewModel.StreamData.Main = windowsData.StreamData.Main.ToLower().Trim();
        playerViewModel.StreamData.Alt = windowsData.StreamData.Alt.ToLower().Trim();
        playerViewModel.StreamData.Other = windowsData.StreamData.Other.ToLower().Trim();
        playerViewModel.StreamData.OtherType = windowsData.StreamData.OtherType;

        Tournament.PresetIsModified();
        await playerViewModel.CompleteData();
        return true;
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = DialogBox.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            int n = Tournament.Players.Count - 1;
            for (var i = n; i >= 0; i--)
            {
                var player = Tournament.Players[i];
                Tournament.RemovePlayer(player);
            }
        });
        
        ClearFilters();
    }
    private void RemoveSelectedPlayer()
    {
        if (SelectedPlayers == null) return;

        int n = SelectedPlayers.Count;
        for (int i = 0; i < n; i++)
        {
            Remove(SelectedPlayers[0]);
        }

        SelectedPlayers.Clear();
    }

    private async Task FixPlayersHeads(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        int count = Tournament.Players.Count;
        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var current = Tournament!.Players[i];
            progress.Report((float)i / count);
            logProgress.Report($"({i+1}/{count}) Checking skin to update {current.InGameName} head");

            await current.ForceUpdateHeadImage();
            await Task.Delay(500);
        }

        Logger.Information("Done fixing players head skins");
        Tournament.PresetIsModified();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SortingType = PlayerSortingType.Name;
        FilterWhitelist(true);
    }
}
