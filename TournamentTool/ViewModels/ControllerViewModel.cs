using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Services;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : SelectableViewModel
{
    private readonly TwitchService _twitch;

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
        get { return _selectedWhitelistPlayer; }
        set
        {
            SidePanel?.ClearSelectedPlayer();
            ClearSelectedWhitelistPlayer();

            _selectedWhitelistPlayer = value;
            OnPropertyChanged(nameof(SelectedWhitelistPlayer));

            CurrentChosenPlayer = value;
            SetPovAfterClickedCanvas();
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


    public ControllerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        MainScene = new(this);
        PreviewScene = new(this);

        OBS = new(this);
        _twitch = new(this);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshScenesPOVS(); });
    }

    public override bool CanEnable(Tournament tournament)
    {
        if (tournament is null) return false;

        Configuration = tournament;
        return true;
    }
    public override void OnEnable(object? parameter)
    {
        foreach (var player in Configuration.Players)
            player.ShowCategory(!Configuration.ShowLiveOnlyForMinecraftCategory && Configuration.IsUsingTwitchAPI);

        switch(Configuration.ControllerMode)
        {
            case ControllerMode.None:
                UseSidePanel = false;

                if (SidePanel != null)
                    SidePanel = null;
                break;
            case ControllerMode.PaceMan:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && !SidePanel.GetType().Equals(typeof(PaceManPanel))))
                    SidePanel = new PaceManPanel(this);
                break;
            case ControllerMode.Ranked:
                UseSidePanel = true;

                if (SidePanel == null || (SidePanel != null && !SidePanel.GetType().Equals(typeof(RankedPacePanel))))
                    SidePanel = new RankedPacePanel(this);
                break;
        }

        FilterItems();

        SidePanel?.OnEnable(null);
        OBS.OnEnable(null);

        if (!Configuration.IsUsingTwitchAPI)
        {
            foreach (var player in Configuration.Players)
                player.StreamData.LiveData.Clear(false);

            return;
        }

        Task.Factory.StartNew(async () =>
        {
            await _twitch.ConnectTwitchAPIAsync();
        });
    }
    public override bool OnDisable()
    {
        SidePanel?.OnDisable();
        OBS.OnDisable();
        _twitch?.OnDisable();

        Configuration.ClearFromController();

        MainScene.Clear();
        PreviewScene.Clear();

        FilteredPlayers!.Clear();
        CurrentChosenPOV = null;
        SelectedWhitelistPlayer = null;
        CurrentChosenPlayer = null;

        return true;
    }

    public void FilterItems()
    {
        //TODO: 0 przebudowac to tak zeby nie czyscic za kazdym razem
        Application.Current.Dispatcher.Invoke(FilteredPlayers.Clear);

        IEnumerable<Player> playersToAdd = [];

        playersToAdd = Configuration.Players
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

    public void SetPovAfterClickedCanvas()
    {
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
        MainViewModel.SavePreset();
    }

    public void ClearScenes()
    {
        MainScene.ClearPovs();
        PreviewScene.ClearPovs();
    }
}
