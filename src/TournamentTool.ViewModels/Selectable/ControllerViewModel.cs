using System.Collections.ObjectModel;
using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Extensions;
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
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Factories;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;
using TournamentTool.ViewModels.Selectable.Controller;
using TournamentTool.ViewModels.Selectable.Controller.Hub;
using TournamentTool.ViewModels.Selectable.Controller.ManagementPanel;
using TournamentTool.ViewModels.Selectable.Controller.SidePanel;

namespace TournamentTool.ViewModels.Selectable;

public class ControllerViewModel : SelectableViewModel, IPovDragAndDropContext, IPlayerAddReceiver, IHotkeyReceiver
{
    private readonly ITwitchService _twitch;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly ITournamentState _tournamentState;
    private readonly IBackgroundCoordinator _backgroundCoordinator;
    public ILoggingService Logger { get; }
    
    public SceneControllerViewModel SceneController { get; }
    public ControllerServiceHub ServiceHub { get; }

    public ReadOnlyObservableCollection<IPlayerViewModel> Players => _playerRepository.Players;
    public Predicate<object> PlayerFilter => FilterPlayers;
    private int _playerViewRefreshTrigger;
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

    private IPlayer? _currentChosenPlayer;
    public IPlayer? CurrentChosenPlayer
    {
        get => _currentChosenPlayer;
        set
        {
            SceneController.CurrentChosenPlayer = value;
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

    private string _twitchUpdateProgressText = string.Empty;
    public string TwitchUpdateProgressText
    {
        get => _twitchUpdateProgressText;
        set
        {
            _twitchUpdateProgressText = value;
            OnPropertyChanged(nameof(TwitchUpdateProgressText));
        }
    }

    private bool _isTwitchAPIConnect;
    public bool IsTwitchAPIConnect
    {
        get => _isTwitchAPIConnect;
        set
        {
            _isTwitchAPIConnect = value;
            OnPropertyChanged(nameof(IsTwitchAPIConnect));
        }
    }

    public ICommand UnSelectItemsCommand { get; set; }
    
    private readonly Domain.Entities.Settings _settings;
    

    public ControllerViewModel(ITournamentPlayerRepository playerRepository, ITournamentState tournamentState, IBackgroundCoordinator backgroundCoordinator,
        ITwitchService twitch, ILoggingService logger, ISettingsProvider settingsProvider, IDispatcherService dispatcher, 
        ISceneControllerViewModelFactory sceneControllerFactory) : base(dispatcher)
    {
        Logger = logger;
        _playerRepository = playerRepository;
        _tournamentState = tournamentState;
        _backgroundCoordinator = backgroundCoordinator;
        _twitch = twitch;
        
        _twitch.ConnectionStateChanged += OnTwitchConnectionChanged;
        
        _settings = settingsProvider.Get<Domain.Entities.Settings>();

        SceneController = sceneControllerFactory.Create();
        ServiceHub = new ControllerServiceHub(this, twitch, logger, playerRepository);

        UnSelectItemsCommand = new RelayCommand(() => { UnSelectItems(true); });
    }

    public override bool CanEnable()
    {
        return _tournamentState.IsCurrentlyOpened;
    } 
    public override void OnEnable(object? parameter)
    {
        SceneController.UnSelectTriggered += OnUnSelectTriggered;
        
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
        
        UpdateTwitchConnectionData(_twitch.IsConnected);
        
        SidePanel?.OnEnable(null);
        ManagementPanel?.OnEnable(null);
        SceneController.OnEnable(null);
        ServiceHub.OnEnable();
        
        _backgroundCoordinator.Register(this);
        _backgroundCoordinator.Register(ManagementPanel);
        _backgroundCoordinator.Register(SidePanel);
    }
    public override bool OnDisable()
    {
        SceneController.UnSelectTriggered -= OnUnSelectTriggered;
        
        _backgroundCoordinator.Unregister(SidePanel);
        _backgroundCoordinator.Unregister(this);
        _backgroundCoordinator.Unregister(ManagementPanel);
        
        SidePanel?.OnDisable();
        ManagementPanel?.OnDisable();
        SceneController.OnDisable();
        ServiceHub.OnDisable();

        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearFromController();

        CurrentChosenPOV = null;
        SelectedWhitelistPlayer = null;
        CurrentChosenPlayer = null;

        return true;
    }

    private void OnUnSelectTriggered(object? sender, UnSelectTriggeredEventArgs e) 
        => UnSelectItems(e.ClearAll);
    private void OnTwitchConnectionChanged(object? sender, ConnectionStateChangedEventArgs e) 
        => UpdateTwitchConnectionData(e.NewState == ConnectionState.Connected);
    
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
        PlayerViewRefreshTrigger++;
    }

    public void SetPovAfterClickedCanvas(IPlayer chosenPlayer)
    {
        CurrentChosenPlayer = chosenPlayer;
        if (CurrentChosenPOV == null || CurrentChosenPlayer == null) return;

        bool isPlayerInPOV = CurrentChosenPOV.Type == SceneType.Main
            ? SceneController.MainScene.ExistInItems<PointOfView>(p =>
                p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo))
            : SceneController.PreviewScene.ExistInItems<PointOfView>(p =>
                p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo));
        if (isPlayerInPOV)
        {
            PointOfView? pov = CurrentChosenPOV.Type == SceneType.Main 
                ? SceneController.MainScene.GetItem<PointOfView>(p => p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo)) 
                : SceneController.PreviewScene.GetItem<PointOfView>(p => p.StreamDisplayInfo.Equals(CurrentChosenPlayer.StreamDisplayInfo));
            if (pov == null) return;
            
            CurrentChosenPOV!.SwapAsync(pov);
            UnSelectItems();
            return;
        }

        CurrentChosenPOV.SetPOVAsync(CurrentChosenPlayer);
        UnSelectItems();
    }
    
    public string GetHeadURL(string id, int size)
    {
        return _settings.HeadAPIType.GetHeadURL(id, size);
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

    private void UpdateTwitchConnectionData(bool isConnected)
    {
        IsTwitchAPIConnect = isConnected;
        ServiceHub.ChangeServiceStatus("Twitch-streams", isConnected);
        
        if (isConnected) return;
        
        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearStreamData();
    }
}
