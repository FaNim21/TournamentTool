using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using MethodTimer;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.ManagementPanels;
using TournamentTool.Modules.OBS;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : SelectableViewModel, IPovDragAndDropContext, IPlayerAddReceiver
{
    private readonly TwitchService _twitch;
    private readonly APIDataSaver _api;

    private readonly IBackgroundCoordinator _backgroundCoordinator;
    
    private BackgroundWorker? _apiWorker;
    private CancellationTokenSource? _cancellationTokenSource;
    public Scene MainScene { get; }
    public PreviewScene PreviewScene { get; }

    public ICollectionView? FilteredPlayersCollectionView { get; private set; }

    public ObsController OBS { get; }

    public SidePanel? SidePanel { get; set; }
    public ManagementPanel? ManagementPanel { get; set; }

    public TournamentViewModel TournamentViewModel { get; }
    public LeaderboardPanelViewModel Leaderboard { get; }
    public IPresetSaver PresetService { get; }

    private IPlayer? _currentChosenPlayer;
    public IPlayer? CurrentChosenPlayer
    {
        get => _currentChosenPlayer;
        set
        {
            _currentChosenPlayer = value;
            OnPropertyChanged(nameof(CurrentChosenPlayer));
        }
    }

    private PlayerViewModel? _selectedWhitelistPlayer;
    public PlayerViewModel? SelectedWhitelistPlayer
    {
        get => _selectedWhitelistPlayer;
        set
        {
            SidePanel?.ClearSelectedPlayer();
            ClearSelectedWhitelistPlayer();

            _selectedWhitelistPlayer = value;
            OnPropertyChanged(nameof(SelectedWhitelistPlayer));

            SetPovAfterClickedCanvas(value!);
        }
    }

    private PointOfView? _currentChosenPOV;
    public PointOfView? CurrentChosenPOV
    {
        get => _currentChosenPOV;
        set
        {
            if (value == null)
                _currentChosenPOV?.UnFocus();
            _currentChosenPOV = value;
            OnPropertyChanged(nameof(CurrentChosenPOV));
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            RefreshFilteredCollection();
            OnPropertyChanged(nameof(SearchText));
        }
    }

    private bool _useSidePanel = true;
    public bool UseSidePanel
    {
        get => _useSidePanel;
        set
        {
            _useSidePanel = value;
            OnPropertyChanged(nameof(UseSidePanel));
        }
    }

    public ICommand RefreshPOVsCommand { get; set; }
    public ICommand UnSelectItemsCommand { get; set; }

    private CancellationTokenSource? _playersRefreshTokenSource;
    

    public ControllerViewModel(ICoordinator coordinator, TournamentViewModel tournamentViewModel, IPresetSaver presetService, LeaderboardPanelViewModel leaderboard, IBackgroundCoordinator backgroundCoordinator) : base(coordinator)
    {
        TournamentViewModel = tournamentViewModel;
        PresetService = presetService;
        Leaderboard = leaderboard;
        _backgroundCoordinator = backgroundCoordinator;

        _api = new APIDataSaver();

        MainScene = new Scene(this, coordinator);
        PreviewScene = new PreviewScene(this, coordinator);

        OBS = new ObsController(this);
        _twitch = new TwitchService(this);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
        UnSelectItemsCommand = new RelayCommand(() => { UnSelectItems(true); });
    }

    public override bool CanEnable()
    {
        return !TournamentViewModel.IsNullOrEmpty();
    } 
    public override void OnEnable(object? parameter)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var collectionViewSource = CollectionViewSource.GetDefaultView(TournamentViewModel.Players);
            collectionViewSource.Filter = null;
            collectionViewSource.Filter = FilterPlayers;
            collectionViewSource.SortDescriptions.Clear();
            collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PlayerViewModel.isStreamLive), ListSortDirection.Descending));
            FilteredPlayersCollectionView = collectionViewSource;
            FilteredPlayersCollectionView.Refresh();
        });
        
        switch(TournamentViewModel.ControllerMode)
        {
            case ControllerMode.None:
                UseSidePanel = false;

                if (SidePanel != null)
                {
                    SidePanel = null;
                    ManagementPanel = null;
                }
                break;
            case ControllerMode.Paceman:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && SidePanel.GetType() != typeof(PaceManPanel)))
                {
                    SidePanel = new PaceManPanel(this);
                    ManagementPanel = null;
                }
                break;
            case ControllerMode.Ranked:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && SidePanel.GetType() != typeof(RankedPacePanel)))
                {
                    SidePanel = new RankedPacePanel(this);
                    ManagementPanel = new RankedManagementPanel((RankedManagementData)TournamentViewModel.ManagementData!);
                }
                break;
        }
        
        _backgroundCoordinator.Register(SidePanel);
        _backgroundCoordinator.Register(this);
        _backgroundCoordinator.Register(ManagementPanel);
        
        _cancellationTokenSource = new CancellationTokenSource();
        _apiWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
        _apiWorker.DoWork += UpdateAPI;
        _apiWorker.RunWorkerAsync();
        
        SidePanel?.OnEnable(null);
        ManagementPanel?.OnEnable(null);
        OBS.OnEnable(null);

        if (!TournamentViewModel.IsUsingTwitchAPI)
        {
            TournamentViewModel.ClearPlayerStreamData();
            return;
        }

        Task.Factory.StartNew(async () => { await _twitch.ConnectTwitchAPIAsync(); });
    }
    public override bool OnDisable()
    {
        _backgroundCoordinator.Unregister(SidePanel);
        _backgroundCoordinator.Unregister(this);
        _backgroundCoordinator.Unregister(ManagementPanel);
        
        SidePanel?.OnDisable();
        OBS.OnDisable();
        _twitch?.OnDisable();
        ManagementPanel?.OnDisable();

        _apiWorker?.CancelAsync();
        _cancellationTokenSource?.Cancel();
        _apiWorker?.Dispose();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _apiWorker = null;

        TournamentViewModel.ClearFromController();

        MainScene.Clear();
        PreviewScene.Clear();

        // FilteredPlayers!.Clear();
        CurrentChosenPOV = null;
        SelectedWhitelistPlayer = null;
        CurrentChosenPlayer = null;

        SavePreset();

        return true;
    }
    
    private async void UpdateAPI(object? sender, DoWorkEventArgs e)
    {
        if (ManagementPanel == null) return;

        ManagementPanel.InitializeAPI(_api);

        var cancellationToken = _cancellationTokenSource!.Token;

        while (!_apiWorker!.CancellationPending && !cancellationToken.IsCancellationRequested)
        {
            ManagementPanel.UpdateAPI(_api);

            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(TournamentViewModel.ApiRefreshRateMiliseconds), cancellationToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    public void Add(PlayerViewModel playerViewModel)
    {
        TournamentViewModel.AddPlayer(playerViewModel);
        
        RefreshFilteredCollection();
    }
    
    private bool FilterPlayers(object obj)
    {
        if (obj is not PlayerViewModel player) return false;

        bool matchesText = string.IsNullOrWhiteSpace(SearchText) || player.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
        var hasStreamData = !player.StreamData.AreBothNullOrEmpty();
        
        return matchesText && hasStreamData;
    }

    public void RefreshFilteredCollection()
    {
        _playersRefreshTokenSource?.Cancel();
        _playersRefreshTokenSource = new CancellationTokenSource();
        var token = _playersRefreshTokenSource.Token;

        Task.Delay(1000).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredPlayersCollectionView?.Refresh();
            });
        }, TaskScheduler.Default);
    }

    public async Task RefreshScenesPOVS()
    { 
        await MainScene.RefreshPovs();
        await PreviewScene.RefreshPovs();
    }
    public async Task RefreshScenes()
    {
        await MainScene.Refresh();
        await PreviewScene.Refresh();
    }

    public void SetPovAfterClickedCanvas(IPlayer chosenPlayer)
    {
        CurrentChosenPlayer = chosenPlayer;
        if (CurrentChosenPOV == null || CurrentChosenPlayer == null) return;

        CurrentChosenPOV.SetPOV(CurrentChosenPlayer);
        UnSelectItems();
    }

    public void UnSelectItems(bool ClearAll = false)
    {
        CurrentChosenPlayer = null;
        SidePanel?.ClearSelectedPlayer();
        ClearSelectedWhitelistPlayer();

        if (ClearAll) CurrentChosenPOV = null;
    }

    public void ClearSelectedWhitelistPlayer()
    {
        _selectedWhitelistPlayer = null;
        OnPropertyChanged(nameof(SelectedWhitelistPlayer));
    }

    public void SavePreset()
    {
        PresetService.SavePreset();
    }

    public void ClearScenes()
    {
        MainScene.ClearPovs();
        PreviewScene.ClearPovs();
    }
}
