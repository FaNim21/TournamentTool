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
    IGN,
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
    
    public ICommand AddPlayerCommand { get; set; }
    public ICommand EditPlayerCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }
    public ICommand FixPlayerHeadCommand { get; set; }

    public ICommand ImportPlayersCommand { get; set; }
    public ICommand ExportPlayersCommand { get; set; }

    public ICommand LoadFromPaceManCommand { get; set; }

    public ICommand RemoveAllPlayerCommand { get; set; }
    public ICommand FixPlayersHeadsCommand { get; set; }
    
    public ICommand SubmitSearchCommand { get; set; }

    private const StringComparison _comparison = StringComparison.OrdinalIgnoreCase;


    public PlayerManagerViewModel(MainViewModelCoordinator coordinator) : base(coordinator)
    {
        AddPlayerCommand = new RelayCommand(AddPlayer);
        EditPlayerCommand = new EditPlayerCommand(this);
        RemovePlayerCommand = new RemovePlayerCommand(this, coordinator);
        FixPlayerHeadCommand = new FixPlayerHeadCommand(this, coordinator, coordinator);

        ImportPlayersCommand = new ImportWhitelistCommand(this, coordinator.MainViewModel.Configuration!, coordinator, coordinator);
        ExportPlayersCommand = new ExportWhitelistCommand(coordinator.MainViewModel.Configuration!);

        LoadFromPaceManCommand = new LoadDataFromPacemanCommand(this, coordinator.MainViewModel.Configuration!, coordinator, coordinator);

        RemoveAllPlayerCommand = new RelayCommand(RemoveAllPlayers);
        FixPlayersHeadsCommand = new RelayCommand( () => { Coordinator.ShowLoading(FixPlayersHeads); });

        SubmitSearchCommand = new RelayCommand(FilterWhitelist);
        
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

        Tournament = tournament;
        return true;
    }
    public override void OnEnable(object? parameter)
    {
        FilterWhitelist();
    }
    public override bool OnDisable()
    {
        ChosenEvent = null;

        return true;
    }

    public void Add(Player player)
    {
        bool wasSearched = false;
        switch (SortingType)
        {
            case PlayerSortingType.Name:
                if (player.Name!.Contains(SearchText, _comparison)) wasSearched = true;
                break;
            case PlayerSortingType.IGN:
                if (player.InGameName!.Contains(SearchText, _comparison)) wasSearched = true;
                break;
            case PlayerSortingType.Stream:
                if (player.StreamData.Main!.Contains(SearchText, _comparison) ||
                    player.StreamData.Alt!.Contains(SearchText, _comparison)) wasSearched = true;
                break;
        }
            
        if (wasSearched) FilteredPlayers.Add(player);
        Tournament.AddPlayer(player);
    }
    public void Remove(Player player)
    {
        FilteredPlayers.Remove(player);
        Tournament.RemovePlayer(player);
    }
    
    public void FilterWhitelist()
    {
        if (string.IsNullOrEmpty(SearchText))
        {
            FilteredPlayers = new ObservableCollection<Player>(Tournament.Players);
            InformationCount = $"Found {FilteredPlayers.Count}/{Tournament.Players.Count}";
            return;
        }
        
        Coordinator.ShowLoading(FilterWhitelist);
    }
    private Task FilterWhitelist(IProgress<float> progress, IProgress<string> logProgress, CancellationToken cancellationToken)
    {
        logProgress.Report("Filtering for results");
        Application.Current.Dispatcher.Invoke(() =>
        {
            FilteredPlayers.Clear();
        });
        var filtered = SortingType switch
        {
            PlayerSortingType.Name => Tournament.Players.Where(p => p.Name!.Contains(SearchText, _comparison)),
            PlayerSortingType.IGN => Tournament.Players.Where(p => p.InGameName!.Contains(SearchText, _comparison)),
            PlayerSortingType.Stream => Tournament.Players.Where(p =>
                p.StreamData.Main!.Contains(SearchText, _comparison) ||
                p.StreamData.Alt.Contains(SearchText, _comparison)),
            _ => []
        };

        progress.Report(0.5f);
        logProgress.Report("Applying results");
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var player in filtered)
            {
                FilteredPlayers.Add(player);
            }
        });
        InformationCount = $"Found {FilteredPlayers.Count}/{Tournament.Players.Count}";
        progress.Report(1);
        return Task.CompletedTask;
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

        player.Name = windowsData.Name;
        player.InGameName = windowsData.InGameName;
        player.PersonalBest = windowsData.PersonalBest;
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
        InformationCount = $"Found {FilteredPlayers.Count}/{Tournament.Players.Count}";
        SavePreset();
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

    public void SavePreset()
    {
        Coordinator.SavePreset();
    }
}
