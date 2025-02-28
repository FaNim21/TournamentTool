using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using MethodTimer;
using TournamentTool.Commands;
using TournamentTool.Commands.PlayerManager;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.Utils;
using TournamentTool.Windows;

namespace TournamentTool.ViewModels;

public enum PlayerSortingType
{
    Name,
    InGameName,
    TeamName,
    Stream
}

public class PlayerManagerViewModel : SelectableViewModel
{
    public ObservableCollection<PaceManEvent> PaceManEvents { get; set; } = [];
     
    private ObservableCollection<Player> _filteredPlayers = [];
    public ObservableCollection<Player> FilteredPlayers
    {
        get => _filteredPlayers;
        set
        {
            _filteredPlayers = value;
            OnPropertyChanged(nameof(FilteredPlayers));
        }
    }

    private Tournament _tournament = new();
    public Tournament Tournament
    {
        get => _tournament;
        set
        {
            _tournament = value;
            OnPropertyChanged(nameof(Tournament));
        }
    }

    private ObservableCollection<Player> _selectedPlayers = [];
    public ObservableCollection<Player> SelectedPlayers
    {
        get => _selectedPlayers;
        set
        {
            _selectedPlayers = value;
            OnPropertyChanged(nameof(SelectedPlayers));
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
    
    public ICommand AddPlayerCommand { get; set; }
    public ICommand EditPlayerCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }
    public ICommand FixPlayerHeadCommand { get; set; }
    
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
    private string _lastTournamentName = string.Empty;
    private bool _isRestartingWhitelist;


    public PlayerManagerViewModel(MainViewModelCoordinator coordinator) : base(coordinator)
    {
        AddPlayerCommand = new RelayCommand(AddPlayer);
        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this, coordinator);
        FixPlayerHeadCommand = new FixPlayerHeadCommand(this, coordinator, coordinator);

        RemoveSelectedPlayerCommand = new RelayCommand(RemoveSelectedPlayer);

        ImportPlayersCommand = new ImportWhitelistCommand(this, coordinator.MainViewModel.Configuration!, coordinator, coordinator);
        ExportPlayersCommand = new ExportWhitelistCommand(coordinator.MainViewModel.Configuration!);

        LoadFromPaceManCommand = new LoadDataFromPacemanCommand(this, coordinator.MainViewModel.Configuration!, coordinator, coordinator);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand( () => { Coordinator.ShowLoading(FixPlayersHeads); });

        SubmitSearchCommand = new RelayCommand(async () => { await FilterWhitelist();});
        ClearSearchFieldCommand = new RelayCommand(ClearFilters);
        
        Task.Run(async () => {
            PaceManEvent[]? eventsData = null;
            try
            {
                string result = await Helper.MakeRequestAsString("https://paceman.gg/api/cs/eventlist");
                eventsData = JsonSerializer.Deserialize<PaceManEvent[]>(result);
            }
            catch
            {
                DialogBox.Show("Can't load paceman events", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); 
            }

            if (eventsData == null) return;

            PaceManEvents = new ObservableCollection<PaceManEvent>(eventsData);
            OnPropertyChanged(nameof(PaceManEvents));
        });
    }

    public override bool CanEnable(Tournament? tournament)
    {
        if (tournament is null) return false;

        if (!tournament.Name.Equals(_lastTournamentName))
        {
            SearchText = string.Empty;
            SortingType = PlayerSortingType.Name;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredPlayers.Clear();
            });
            _isRestartingWhitelist = true;
        }
        else
        {
            _isRestartingWhitelist = false;
        }
        
        Tournament = tournament;
        _lastTournamentName = tournament.Name;
        
        return true;
    }
    public override void OnEnable(object? parameter)
    {
        if (!_isRestartingWhitelist) return;
        _ = FilterWhitelist(true);
    }
    public override bool OnDisable()
    {
        ChosenEvent = null;

        return true;
    }

    public void Add(Player player)
    {
        bool wasSearched = false;
        if (!string.IsNullOrEmpty(SearchText))
        {
            switch (SortingType)
            {
                case PlayerSortingType.Name:
                    if (player.Name!.Contains(SearchText, _comparison)) wasSearched = true;
                    break;
                case PlayerSortingType.InGameName:
                    if (player.InGameName!.Contains(SearchText, _comparison)) wasSearched = true;
                    break;
                case PlayerSortingType.TeamName:
                    if (player.TeamName!.Contains(SearchText, _comparison)) wasSearched = true;
                    break;
                case PlayerSortingType.Stream:
                    if (player.StreamData.Main!.Contains(SearchText, _comparison) ||
                        player.StreamData.Alt!.Contains(SearchText, _comparison)) wasSearched = true;
                    break;
            }
        }
        else wasSearched = true;
        
        if (wasSearched) FilteredPlayers.Add(player);
        Tournament.AddPlayer(player);
        
        UpdateInformationCountText();
    }
    public void Remove(Player player)
    {
        FilteredPlayers.Remove(player);
        Tournament.RemovePlayer(player);
        
        UpdateInformationCountText();
    }
    
    public async Task FilterWhitelist(bool forceFilter = false)
    {
        if (!forceFilter && _lastFilterSearch.Equals(SearchText, _comparison) && _lastSortingType.Equals(SortingType)) return;
        if (!forceFilter && string.IsNullOrEmpty(SearchText) && string.IsNullOrEmpty(_lastFilterSearch)) return;

        _lastFilterSearch = SearchText;
        _lastSortingType = SortingType;
        IsSearchEnabled = false;
        
        if (string.IsNullOrEmpty(SearchText))
        {
            UpdateInformationCountText("Restoring...", "?");
            await Task.Delay(1);
            
            FilteredPlayers = new ObservableCollection<Player>(Tournament.Players);
            
            UpdateInformationCountText();
            IsSearchEnabled = true;
            return;
        }
        UpdateInformationCountText("Filtering...", "?");
        await Task.Delay(1);
        
        var filtered = SortingType switch
        {
            PlayerSortingType.Name => Tournament.Players.Where(p => p.Name!.Trim().Contains(SearchText.Trim(), _comparison)),
            PlayerSortingType.InGameName => Tournament.Players.Where(p => p.InGameName!.Trim().Contains(SearchText.Trim(), _comparison)),
            PlayerSortingType.TeamName => Tournament.Players.Where(p => p.TeamName!.Trim().Contains(SearchText.Trim(), _comparison)),
            PlayerSortingType.Stream => Tournament.Players.Where(p =>
                p.StreamData.Main!.Trim().Contains(SearchText.Trim(), _comparison) ||
                p.StreamData.Alt.Trim().Contains(SearchText.Trim(), _comparison)),
            _ => []
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            FilteredPlayers = new ObservableCollection<Player>(filtered);
        });
        
        UpdateInformationCountText();
        IsSearchEnabled = true;
    }
    
    private void UpdateInformationCountText(string header = "Found", string filteredCount = "")
    {
        if (string.IsNullOrEmpty(filteredCount))
            filteredCount = FilteredPlayers.Count.ToString();
        
        InformationCount = $"{header} {filteredCount}/{Tournament.Players.Count}";
    }
    
    private void AddPlayer()
    {
        AddPlayer(null);
    }
    public void AddPlayer(Player? player)
    {
        WhitelistPlayerWindowViewModel viewModel = new(this, player);
        WhitelistPlayerWindow window = new(viewModel);

        Coordinator.ShowDialog(window);
    }

    public async Task<bool> SavePlayer(Player player, bool isEditing)
    {
        if (string.IsNullOrEmpty(player.Name))
        {
            DialogBox.Show($"Name cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (string.IsNullOrEmpty(player.InGameName))
        {
            DialogBox.Show($"InGameName cannot be empty", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (isEditing)
        {
            if (!await EditPlayer(player)) return false;
        }
        else
        {
            if (!await AddPlayerToWhitelist(player)) return false;
        }

        SavePreset();
        return true;
    }
    private async Task<bool> EditPlayer(Player player)
    {
        int n = Tournament!.Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Tournament!.Players[i];
            if (!current.Id.Equals(player.Id)) continue;
            
            bool success = await UpdatePlayerData(current, player, player.Id);
            return success;
        }
        return true;
    }
    private async Task<bool> AddPlayerToWhitelist(Player player)
    {
        Player newPlayer = new();
        bool success = await UpdatePlayerData(newPlayer, player);
        if (!success) return false;

        Add(newPlayer);
        return true;
    }
    private async Task<bool> UpdatePlayerData(Player player, Player windowsData, Guid? excludeID = null)
    {
        if (Tournament.ContainsDuplicates(windowsData, excludeID)) return false;

        player.Name = windowsData.Name!.Trim();
        player.InGameName = windowsData.InGameName!.Trim();
        player.PersonalBest = windowsData.PersonalBest;
        player.TeamName = windowsData.TeamName?.Trim();
        player.StreamData.Main = windowsData.StreamData.Main.ToLower().Trim();
        player.StreamData.Alt = windowsData.StreamData.Alt.ToLower().Trim();

        await player.UpdateHeadImage();
        return true;
    }

    private void RemoveAllPlayers()
    {
        MessageBoxResult result = DialogBox.Show("Are you sure you want to remove all players from whitelist?", "Removing players", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        Tournament!.Players.Clear();
        FilteredPlayers.Clear();
        
        UpdateInformationCountText();
        SavePreset();
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
        }

        DialogBox.Show("Done fixing players head skins");
        SavePreset();
    }

    private void ClearFilters()
    {
        SearchText = string.Empty;
        SortingType = PlayerSortingType.Name;
        _ = FilterWhitelist();
    }
    
    public void SavePreset()
    {
        Coordinator.SavePreset();
    }
}
