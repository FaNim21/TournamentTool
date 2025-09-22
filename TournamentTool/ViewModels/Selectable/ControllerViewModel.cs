using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Controller;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.ManagementPanels;
using TournamentTool.Modules.OBS;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Controller;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.Selectable;

public class ControllerViewModel : SelectableViewModel, IPovDragAndDropContext, IPlayerAddReceiver
{
    private readonly TwitchService _twitch;

    private readonly IBackgroundCoordinator _backgroundCoordinator;
    
    public SceneControllerViewmodel SceneController { get; }
    private readonly ControllerServiceHub _serviceHub;

    public ICollectionView? FilteredPlayersCollectionView { get; private set; }

    public SidePanel? SidePanel { get; set; }
    public ManagementPanel? ManagementPanel { get; set; }

    public TournamentViewModel TournamentViewModel { get; }
    public LeaderboardPanelViewModel Leaderboard { get; }
    public ILoggingService Logger { get; }
    public ISettings SettingsService { get; }

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

    public PointOfView? CurrentChosenPOV
    {
        get => SceneController.CurrentChosenPOV;
        set => SceneController.CurrentChosenPOV = value;
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

    public string _twitchUpdateProgressText = string.Empty;
    public string TwitchUpdateProgressText
    {
        get => _twitchUpdateProgressText;
        set
        {
            
            _twitchUpdateProgressText = value;
            OnPropertyChanged(nameof(TwitchUpdateProgressText));
        }
    }

    public bool IsUsingTwitchAPI => TournamentViewModel.IsUsingTwitchAPI;

    public ICommand UnSelectItemsCommand { get; set; }
    
    private CancellationTokenSource? _playersRefreshTokenSource;
    

    public ControllerViewModel(ICoordinator coordinator, 
        TournamentViewModel tournamentViewModel,
        LeaderboardPanelViewModel leaderboard, 
        IBackgroundCoordinator backgroundCoordinator, 
        ObsController obs,
        TwitchService twitch, 
        ILoggingService logger,
        ISettings settingsService) : base(coordinator)
    {
        TournamentViewModel = tournamentViewModel;
        Leaderboard = leaderboard;
        Logger = logger;
        SettingsService = settingsService;
        _backgroundCoordinator = backgroundCoordinator;
        _twitch = twitch;
        
        SceneController = new SceneControllerViewmodel(this, coordinator, obs, tournamentViewModel, logger, settingsService);
        _serviceHub = new ControllerServiceHub(this, twitch, logger, tournamentViewModel, obs);

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
            collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PlayerViewModel.IsStreamLive), ListSortDirection.Descending));
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
        
        SidePanel?.OnEnable(null);
        ManagementPanel?.OnEnable(null);
        SceneController.OnEnable(null);
        _serviceHub.OnEnable();
        
        _backgroundCoordinator.Register(this);
        _backgroundCoordinator.Register(ManagementPanel);
        _backgroundCoordinator.Register(SidePanel);

        if (!IsUsingTwitchAPI || !_twitch.IsConnected)
        {
            TournamentViewModel.ClearPlayerStreamData();
        }
    }
    public override bool OnDisable()
    {
        _backgroundCoordinator.Unregister(SidePanel);
        _backgroundCoordinator.Unregister(this);
        _backgroundCoordinator.Unregister(ManagementPanel);
        
        SidePanel?.OnDisable();
        ManagementPanel?.OnDisable();
        SceneController.OnDisable();
        _serviceHub.OnDisable();

        TournamentViewModel.ClearFromController();

        // FilteredPlayers!.Clear();
        CurrentChosenPOV = null;
        SelectedWhitelistPlayer = null;
        CurrentChosenPlayer = null;

        return true;
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
        var hasStreamData = !player.StreamData.IsNullOrEmpty();
        
        return matchesText && hasStreamData;
    }

    public void RefreshFilteredCollection()
    {
        _playersRefreshTokenSource?.Cancel();
        _playersRefreshTokenSource = new CancellationTokenSource();
        var token = _playersRefreshTokenSource.Token;

        try
        {
            Task.Delay(1000, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
            
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FilteredPlayersCollectionView?.Refresh();
                });
            }, TaskScheduler.Default);
        }
        catch { /**/ }
    }

    public void SetPovAfterClickedCanvas(IPlayer chosenPlayer)
    {
        CurrentChosenPlayer = chosenPlayer;
        if (CurrentChosenPOV == null || CurrentChosenPlayer == null) return;

        bool isPlayerInPOV = CurrentChosenPOV.Type == SceneType.Main ?
            SceneController.MainScene.IsPlayerInPov(CurrentChosenPlayer.StreamDisplayInfo) :
            SceneController.PreviewScene.IsPlayerInPov(CurrentChosenPlayer.StreamDisplayInfo);
        if (isPlayerInPOV)
        {
            var pov = CurrentChosenPOV.Type == SceneType.Main
                ? SceneController.MainScene.GetPlayerPov(CurrentChosenPlayer.StreamDisplayInfo.Name, CurrentChosenPlayer.StreamDisplayInfo.Type)
                : SceneController.PreviewScene.GetPlayerPov(CurrentChosenPlayer.StreamDisplayInfo.Name, CurrentChosenPlayer.StreamDisplayInfo.Type);
            if (pov == null) return;
            
            CurrentChosenPOV!.Swap(pov);
            UnSelectItems();
            return;
        }

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
}
