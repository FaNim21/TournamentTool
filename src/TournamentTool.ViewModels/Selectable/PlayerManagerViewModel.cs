using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Background;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Commands.PlayerManager;
using TournamentTool.ViewModels.Entities.Player;

namespace TournamentTool.ViewModels.Selectable;

public class PlayerManagerViewModel : SelectableViewModel, IPlayerAddReceiver
{
    private readonly ITournamentState _tournamentState;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    public ITournamentPlayerRepository PlayerRepository { get; }
    public IPresetSaver PresetService { get; }
    private IBackgroundCoordinator BackgroundCoordinator { get; }
    public ILoggingService Logger { get; }
    public IPlayerViewModelFactory PlayerViewModelFactory { get; }
    
    public ObservableCollection<PaceManEvent> PaceManEvents { get; set; } = [];

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
    
    public ReadOnlyObservableCollection<IPlayerViewModel> Players => PlayerRepository.Players;
    public Predicate<object> PlayerFilter => FilterPlayers;
    private int _playerViewRefreshTrigger = 0;
    public int PlayerViewRefreshTrigger
    {
        get => _playerViewRefreshTrigger;
        set
        {
            _playerViewRefreshTrigger = value;
            OnPropertyChanged(nameof(PlayerViewRefreshTrigger));
        }
    }

    private int _filteredCount;
    public int FilteredCount
    {
        get => _filteredCount;
        set
        {
            _filteredCount = value;
            InformationCount = $"Found {_filteredCount}/{PlayerRepository.Players.Count}";
        }
    }

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
            if (value.Equals(_searchText)) return;
            
            _searchText = value;
            OnPropertyChanged(nameof(SearchText));
            _debounceFilteringWhitelist();

            IsSearchEmpty = string.IsNullOrEmpty(SearchText);
        }
    }
    
    private PlayerSortingType _sortingType = PlayerSortingType.All;
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

    private Action _debounceFilteringWhitelist;
    
    private const StringComparison _comparison = StringComparison.OrdinalIgnoreCase;
    private string _lastFilterSearch = "filter";
    private PlayerSortingType _lastSortingType;

    private bool _isWhitelistWindowOpened;


    public PlayerManagerViewModel(ITournamentPlayerRepository playerRepository, ITournamentState tournamentState, 
        IPresetSaver presetService, IBackgroundCoordinator backgroundCoordinator, ILoggingService logger, IPlayerViewModelFactory playerViewModelFactory, 
        IPacemanAPIService pacemanApiService, IWindowService windowService, IDispatcherService dispatcher, IClipboardService clipboard, 
        IDialogService dialogService) : base(dispatcher)
    {
        PlayerRepository = playerRepository;
        PresetService = presetService;
        BackgroundCoordinator = backgroundCoordinator;
        Logger = logger;
        PlayerViewModelFactory = playerViewModelFactory;
        _tournamentState = tournamentState;
        _windowService = windowService;
        _dialogService = dialogService;

        _debounceFilteringWhitelist = ((Action)FilterWhitelist).Debounce();
        
        AddPlayerCommand = new RelayCommand(AddPlayer);
        ValidatePlayersCommand = new RelayCommand(() => { windowService.ShowLoading(ValidateAllPlayers); });

        ViewPlayerInfoCommand = new RelayCommand<PlayerViewModel>(player =>
        {
            ViewWhitelistPlayerViewModel viewModel = new(player, presetService, logger, dispatcher, windowService, clipboard, dialogService);
            _windowService.ShowCustomDialog(viewModel);
        });
        EditPlayerCommand = new EditPlayerCommand(this);
        ValidatePlayerCommand = new ValidatePlayerCommand(this, presetService, windowService);
        FixPlayerHeadCommand = new FixPlayerHeadCommand(this, presetService, windowService);
        RemovePlayerCommand = new RemovePlayerCommand(this, presetService, dialogService);

        RemoveSelectedPlayerCommand = new RelayCommand(RemoveSelectedPlayer);

        ImportPlayersCommand = new ImportWhitelistCommand(this, playerRepository, presetService, logger, windowService, dispatcher, dialogService);
        ExportPlayersCommand = new ExportWhitelistCommand(playerRepository, tournamentState, dialogService);

        LoadFromPaceManCommand = new LoadDataFromPacemanCommand(this, playerRepository, presetService, logger, windowService, dispatcher);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand( () => { windowService.ShowLoading(FixPlayersHeads); });

        SubmitSearchCommand = new RelayCommand(FilterWhitelist);
        ClearSearchFieldCommand = new RelayCommand(ClearFilters);

        Dispatcher.Invoke(async () =>
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
        }, CustomDispatcherPriority.Background);
    }

    public override bool CanEnable()
    {
        return _tournamentState.IsCurrentlyOpened;
    } 
    public override void OnEnable(object? parameter)
    {
        BackgroundCoordinator.Register(this);
        FilterWhitelist(true);
    }
    public override bool OnDisable()
    {
        BackgroundCoordinator.Unregister(this);
        PaceManEvents.Clear();
        SelectedPlayers.Clear();
        ChosenEvent = null;
        ShowPlayers = false;
        
        return true;
    }

    private async Task ValidateAllPlayers(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
       int count = PlayerRepository.Players.Count;
       for (int i = 0; i < count; i++)
       {
           var player = PlayerRepository.Players[i];
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

    public void Add(IPlayerViewModel playerViewModel)
    {
        PlayerRepository.AddPlayer(playerViewModel);
    } 
    public void Remove(PlayerViewModel playerViewModel)
    {
        PlayerRepository.RemovePlayer(playerViewModel);
    }
    
    private bool FilterPlayers(object obj)
    {
        if (obj is not PlayerViewModel player) return false;
        if (string.IsNullOrWhiteSpace(_searchText)) return true;

        return SortingType switch
        {
            PlayerSortingType.All => MatchesAny(player),
            PlayerSortingType.Name => player.Name?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.InGameName => player.InGameName?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.TeamName => player.TeamName?.Trim().Contains(SearchText, _comparison) ?? false,
            PlayerSortingType.Stream =>
                (player.StreamData.Main?.Trim().Contains(SearchText, _comparison) ?? false) ||
                (player.StreamData.Alt?.Trim().Contains(SearchText, _comparison) ?? false) ||
                (player.StreamData.Other?.Trim().Contains(SearchText, _comparison) ?? false),
            PlayerSortingType.uuid => player.UUID.Contains(SearchText, _comparison),
            _ => false
        };
    }
    private bool MatchesAny(PlayerViewModel player)
    {
        return 
            (player.Name?.Trim().Contains(SearchText, _comparison) ?? false) ||
            (player.InGameName?.Trim().Contains(SearchText, _comparison) ?? false) ||
            (player.TeamName?.Trim().Contains(SearchText, _comparison) ?? false) ||
            (player.StreamData.Main?.Trim().Contains(SearchText, _comparison) ?? false) ||
            (player.StreamData.Alt?.Trim().Contains(SearchText, _comparison) ?? false) ||
            (player.StreamData.Other?.Trim().Contains(SearchText, _comparison) ?? false) ||
            player.UUID.Contains(SearchText, _comparison);
    }

    public void FilterWhitelist()
    {
        FilterWhitelist(false);
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

        RefreshFilteredCollectionView();
        
        IsSearchEnabled = true;
        ShowPlayers = true;
    }

    public void RefreshFilteredCollectionView()
    {
        PlayerViewRefreshTrigger++;
    }
    
    private void AddPlayer()
    {
        AddPlayer(null);
    }
    public void AddPlayer(Player? player)
    {
        if (_isWhitelistWindowOpened) return;
        _isWhitelistWindowOpened = true;
        
        PlayerViewModel? playerViewModel = null;
        if (player != null)
        {
            IPlayerViewModel createdPlayer = PlayerViewModelFactory.Create(player);
            playerViewModel = createdPlayer as PlayerViewModel;
        }
        
        WhitelistPlayerWindowViewModel viewModel = new(PlayerViewModelFactory, this, playerViewModel, Dispatcher);
        _windowService.ShowCustomDialog(viewModel, _ => { _isWhitelistWindowOpened = false;}, "WhitelistPlayerWindow");
    }

    public async Task<bool> SavePlayer(PlayerViewModel playerViewModel, bool isEditing)
    {
        if (string.IsNullOrEmpty(playerViewModel.Name?.Trim()))
        {
            _dialogService.Show($"Name cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(playerViewModel.InGameName))
        {
            _dialogService.Show($"InGameName cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        int n = PlayerRepository.Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = PlayerRepository.Players[i];
            if (!current.Id.Equals(playerViewModel.Id)) continue;
            
            bool success = await UpdatePlayerData(current, playerViewModel, playerViewModel.Id);
            return success;
        }
        return false;
    }
    private async Task<bool> AddPlayerToWhitelist(PlayerViewModel playerViewModel)
    {
        IPlayerViewModel createdPlayer = PlayerViewModelFactory.Create();
        if (createdPlayer is not PlayerViewModel newPlayerViewModel) return false;
        
        bool success = await UpdatePlayerData(newPlayerViewModel, playerViewModel);
        if (!success) return false;

        Add(newPlayerViewModel);
        RefreshFilteredCollectionView();
        return true;
    }
    private async Task<bool> UpdatePlayerData(IPlayerViewModel playerViewModel, IPlayerViewModel windowsData, Guid? excludeID = null)
    {
        if (PlayerRepository.ContainsDuplicates(windowsData.Data, excludeID)) return false;

        playerViewModel.UpdateData(windowsData);

        _tournamentState.MarkAsModified();
        await playerViewModel.CompleteData();
        return true;
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = _dialogService.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        Dispatcher.Invoke(() =>
        {
            int n = PlayerRepository.Players.Count - 1;
            for (var i = n; i >= 0; i--)
            {
                var player = PlayerRepository.Players[i];
                PlayerRepository.RemovePlayer(player);
            }
        });
        
        RefreshFilteredCollectionView();
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

        RefreshFilteredCollectionView();
        SelectedPlayers.Clear();
    }

    private async Task FixPlayersHeads(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        int count = PlayerRepository.Players.Count;
        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var current = PlayerRepository.Players[i];
            progress.Report((float)i / count);
            logProgress.Report($"({i+1}/{count}) Checking skin to update {current.InGameName} head");

            await current.ForceUpdateHeadImage();
            await Task.Delay(500);
        }

        Logger.Information("Done fixing players head skins");
        _tournamentState.MarkAsModified();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SortingType = PlayerSortingType.All;
        FilterWhitelist(true);
    }
}
