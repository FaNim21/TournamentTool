using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.Modules.ManagementPanels;
using TournamentTool.Modules.OBS;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : SelectableViewModel
{
    private readonly TwitchService _twitch;
    private readonly APIDataSaver _api;

    private BackgroundWorker? _apiWorker;
    private CancellationTokenSource? _cancellationTokenSource;
    public Scene MainScene { get; set; }
    public PreviewScene PreviewScene { get; set; }

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

    public ObsController OBS { get; set; }

    public SidePanel? SidePanel { get; set; }
    public ManagementPanel? ManagementPanel { get; set; }

    public Tournament Configuration { get; private set; } = new();

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

    private Player? _selectedWhitelistPlayer;
    public Player? SelectedWhitelistPlayer
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
            FilterItems();
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


    public ControllerViewModel(MainViewModelCoordinator coordinator) : base(coordinator)
    {
        _api = new APIDataSaver();

        MainScene = new Scene(this, coordinator);
        PreviewScene = new PreviewScene(this, coordinator);

        OBS = new ObsController(this);
        _twitch = new TwitchService(this);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
        UnSelectItemsCommand = new RelayCommand(() => { UnSelectItems(true); });
    }

    public override bool CanEnable(Tournament? tournament)
    {
        if (tournament is null) return false;

        Configuration = tournament;
        return true;
    }
    public override void OnEnable(object? parameter)
    {
        switch(Configuration.ControllerMode)
        {
            case ControllerMode.None:
                UseSidePanel = false;

                if (SidePanel != null)
                {
                    SidePanel = null;
                    ManagementPanel = null;
                }
                break;
            case ControllerMode.PaceMan:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && !SidePanel.GetType().Equals(typeof(PaceManPanel))))
                {
                    SidePanel = new PaceManPanel(this);
                    ManagementPanel = null;
                }
                break;
            case ControllerMode.Ranked:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && !SidePanel.GetType().Equals(typeof(RankedPacePanel))))
                {
                    SidePanel = new RankedPacePanel(this);
                    ManagementPanel = new RankedManagementPanel(this, (RankedPacePanel)SidePanel);
                }
                break;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _apiWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
        _apiWorker.DoWork += UpdateAPI;
        _apiWorker.RunWorkerAsync();

        FilterItems();

        SidePanel?.OnEnable(null);
        ManagementPanel?.OnEnable(null);
        OBS.OnEnable(null);

        if (!Configuration.IsUsingTwitchAPI)
        {
            Configuration.ClearPlayerStreamData();
            return;
        }

        Task.Factory.StartNew(async () => { await _twitch.ConnectTwitchAPIAsync(); });
    }
    public override bool OnDisable()
    {
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

        Configuration.ClearFromController();

        MainScene.Clear();
        PreviewScene.Clear();

        FilteredPlayers!.Clear();
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
                await Task.Delay(TimeSpan.FromMilliseconds(Configuration.ApiRefreshRateMiliseconds), cancellationToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    public void FilterItems()
    {
        //TODO: 0 przebudowac to tak zeby nie czyscic za kazdym razem
        Application.Current.Dispatcher.Invoke(FilteredPlayers.Clear);

        IEnumerable<Player> playersToAdd = Configuration.Players
            .Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) && !player.StreamData.AreBothNullOrEmpty())
            .OrderByDescending(player => player.StreamData.LiveData.Status.Equals("live"));

        foreach (var player in playersToAdd)
            Application.Current.Dispatcher.Invoke(() => FilteredPlayers.Add(player));
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
        Coordinator.SavePreset();
    }

    public void ClearScenes()
    {
        MainScene.ClearPovs();
        PreviewScene.ClearPovs();
    }
}
