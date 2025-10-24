using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Selectable.Controller;
using TournamentTool.ViewModels.Selectable.Controller.Hub;
using TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;
using TournamentTool.ViewModels.Selectable.Controller.SidePanel;

namespace TournamentTool.ViewModels.Selectable;

public class ControllerViewModel : SelectableViewModel, IPovDragAndDropContext, IPlayerAddReceiver, IHotkeyReceiver
{
    private readonly TwitchService _twitch;

    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly ITournamentState _tournamentState;
    private readonly IBackgroundCoordinator _backgroundCoordinator;
    
    public SceneControllerViewmodel SceneController { get; }
    private readonly ControllerServiceHub _serviceHub;

    public ReadOnlyObservableCollection<IPlayerViewModel> Players => _playerRepository.Players;
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

    public SidePanel? SidePanel { get; set; }
    public ManagementPanel? ManagementPanel { get; set; }

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

    public bool IsUsingTwitchAPI => _tournamentState.CurrentPreset.IsUsingTwitchAPI;

    public ICommand UnSelectItemsCommand { get; set; }
    
    private CancellationTokenSource? _playersRefreshTokenSource;
    

    public ControllerViewModel(ICoordinator coordinator, 
        ITournamentPlayerRepository playerRepository,
        ITournamentState tournamentState,
        LeaderboardPanelViewModel leaderboard, 
        IBackgroundCoordinator backgroundCoordinator, 
        ObsController obs,
        TwitchService twitch, 
        ILoggingService logger,
        ISettings settingsService,
        IDispatcherService dispatcher,
        IWindowService windowService) : base(coordinator, dispatcher)
    {
        Leaderboard = leaderboard;
        Logger = logger;
        SettingsService = settingsService;
        _playerRepository = playerRepository;
        _tournamentState = tournamentState;
        _backgroundCoordinator = backgroundCoordinator;
        _twitch = twitch;
        
        SceneController = new SceneControllerViewmodel(this, obs, playerRepository, tournamentState, logger, settingsService, dispatcher, windowService);
        _serviceHub = new ControllerServiceHub(this, twitch, logger, tournamentSerwisyTutaj, obs);

        UnSelectItemsCommand = new RelayCommand(() => { UnSelectItems(true); });
    }

    public override bool CanEnable()
    {
        return !_tournamentState.IsCurrentlyOpened;
    } 
    public override void OnEnable(object? parameter)
    {
        switch(_tournamentState.CurrentPreset.ControllerMode)
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
                    SidePanel = new PaceManPanel(this, Dispatcher);
                    ManagementPanel = null;
                }
                break;
            case ControllerMode.Ranked:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && SidePanel.GetType() != typeof(RankedPacePanel)))
                {
                    SidePanel = new RankedPacePanel(this, Dispatcher);
                    ManagementPanel = new RankedManagementPanel((RankedManagementData)_tournamentState.CurrentPreset.ManagementData!, Dispatcher);
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
            _playerRepository.ClearPlayerStreamData();
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

        _playerRepository.ClearFromController();

        // FilteredPlayers!.Clear();
        CurrentChosenPOV = null;
        SelectedWhitelistPlayer = null;
        CurrentChosenPlayer = null;

        return true;
    }

    public void OnHotkey(HotkeyActionType actionType)
    {
        switch (actionType)
        {
            case HotkeyActionType.Controller_ToggleStudioMode: 
                SceneController.SwitchStudioModeCommand.Execute(null);
                break;
            //...
        }
    }
    
    public void Add(IPlayerViewModel playerViewModel)
    {
        _playerRepository.AddPlayer(playerViewModel);
        
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

        //TODO: 3 ???????????? to jest do weryfikacji i testow, bo jest giga glupie
        try
        {
            Task.Delay(1000, token).ContinueWith(_ =>
            {
                if (token.IsCancellationRequested) return;
            
                Dispatcher.Invoke(() =>
                {
                    PlayerViewRefreshTrigger++;
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
