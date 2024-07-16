using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : SelectableViewModel
{
    private readonly TwitchService _twitch;

    public HttpClient? WebServer { get; set; }

    public ObservableCollection<PointOfView> POVs { get; set; } = [];

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

    public ObsController ObsController { get; set; }
    public PaceManService PaceManService { get; set; }

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
            PaceManService.ClearSelectedPaceManPlayer();
            ClearSelectedWhitelistPlayer();

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

    private float _canvasWidth;
    public float CanvasWidth
    {
        get => _canvasWidth;
        set
        {
            if (_canvasWidth == value) return;

            _canvasWidth = value;
            OnPropertyChanged(nameof(CanvasWidth));
            ResizeCanvas();
        }
    }

    private float _canvasHeight;
    public float CanvasHeight
    {
        get => _canvasHeight;
        set
        {
            if (_canvasHeight == value) return;

            _canvasHeight = value;
            OnPropertyChanged(nameof(CanvasHeight));
            ResizeCanvas();
        }
    }

    public ICommand RefreshPOVsCommand { get; set; }


    public ControllerViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        ObsController = new(this);
        PaceManService = new(this);
        _twitch = new(this);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshPovs(); });
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

        FilterItems();

        PaceManService.OnEnable(null);
        ObsController.OnEnable(null);

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
        PaceManService.OnDisable();
        ObsController.OnDisable();
        _twitch?.OnDisable();

        Configuration.ClearFromController();

        POVs.Clear();
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

    private async Task RefreshPovs()
    {
        for (int i = 0; i < POVs.Count; i++)
        {
            await POVs[i].Refresh();
        }
    }
    public void AddPov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate { POVs.Add(pov); });
        OnPropertyChanged(nameof(POVs));
    }
    public void RemovePov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate { POVs.Remove(pov); });
        OnPropertyChanged(nameof(POVs));
    }
    public void ClearPovs()
    {

        POVs.Clear();
    }

    public bool IsPlayerInPov(string twitchName)
    {
        if (string.IsNullOrEmpty(twitchName)) return false;

        for (int i = 0; i < POVs.Count; i++)
        {
            var current = POVs[i];
            if (current.TwitchName.Equals(twitchName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public void ResizeCanvas()
    {
        if (CanvasWidth == 0 || CanvasHeight == 0) return;

        float calculatedHeight = CanvasWidth / Consts.AspectRatio;
        float calculatedWidth = CanvasHeight * Consts.AspectRatio;

        if (float.IsNaN(calculatedHeight) || float.IsInfinity(calculatedHeight) || float.IsNaN(calculatedWidth) || float.IsInfinity(calculatedWidth)) return;

        if (calculatedHeight > CanvasHeight)
            CanvasWidth = calculatedWidth;
        else
            CanvasHeight = calculatedHeight;
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

        PaceManService.ClearSelectedPaceManPlayer();
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
}
