using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Controller;

public class ControllerViewModel : BaseViewModel
{
    private MainViewModel MainViewModel { get; set; }

    private readonly TwitchService _twitch;

    private BackgroundWorker? _paceManWorker;

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

    private ObservableCollection<PaceMan> _paceManPlayers = [];
    public ObservableCollection<PaceMan> PaceManPlayers
    {
        get => _paceManPlayers;
        set
        {
            _paceManPlayers = value;
            OnPropertyChanged(nameof(PaceManPlayers));
        }
    }

    public ICollectionView? GroupedPaceManPlayers { get; set; }

    public ObsController ObsController { get; set; }

    public Tournament Configuration { get; private set; }

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
            _selectedPaceManPlayer = null;
            OnPropertyChanged(nameof(SelectedPaceManPlayer));

            _selectedWhitelistPlayer = value;
            OnPropertyChanged(nameof(SelectedWhitelistPlayer));

            CurrentChosenPlayer = value;

            SetPovAfterClickedCanvas();
        }
    }

    private PaceMan? _selectedPaceManPlayer;
    public PaceMan? SelectedPaceManPlayer
    {
        get { return _selectedPaceManPlayer; }
        set
        {
            _selectedWhitelistPlayer = null;
            OnPropertyChanged(nameof(SelectedWhitelistPlayer));

            _selectedPaceManPlayer = value;
            OnPropertyChanged(nameof(SelectedPaceManPlayer));

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

    public ICommand GoBackCommand { get; set; }
    public ICommand RefreshPOVsCommand { get; set; }

    //TODO: 0 rozbic pov i paceman update do odpowiadajacych im serwisow
    public ControllerViewModel(MainViewModel mainViewModel)
    {
        Configuration = mainViewModel.PresetManager.CurrentChosen!;
        MainViewModel = mainViewModel;
        GoBackCommand = new RelayCommand(GoBack);

        ObsController = new(this);
        _twitch = new(this);

        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshPovs(); });

        /*Google.Apis.YouTube.v3.LiveStreamsResource.ListRequest
        Google.Apis.Auth.OAuth2.ExternalAccountCredential*/
    }

    public override void OnEnable(object? parameter)
    {
        if (parameter != null && parameter is Tournament tournament)
        {
            Configuration = tournament;
        }

        foreach (var player in Configuration.Players)
            player.ShowCategory(!Configuration.ShowLiveOnlyForMinecraftCategory && Configuration.IsUsingTwitchAPI);

        if (!Configuration.IsUsingTwitchAPI)
        {
            foreach (var player in Configuration.Players)
                player.StreamData.LiveData.Update(new TwitchStreamData(), false);
        }

        FilterItems();
        SetupPaceManGrouping();

        if (Configuration.IsUsingPaceMan)
        {
            _paceManWorker = new() { WorkerSupportsCancellation = true };
            _paceManWorker.DoWork += PaceManUpdate;
            _paceManWorker.RunWorkerAsync();
        }

        Task.Factory.StartNew(async () =>
        {
            await ObsController.Connect(Configuration.Password!, Configuration.Port);
            await _twitch.ConnectTwitchAPIAsync();
        });
    }
    public override bool OnDisable()
    {
        _paceManWorker?.CancelAsync();
        _paceManWorker?.Dispose();

        _twitch?.Dispose();

        for (int i = 0; i < Configuration.Players.Count; i++)
        {
            var current = Configuration.Players[i];
            current.StreamData.LiveData.StatusLabelColor = null;
        }

        POVs.Clear();
        FilteredPlayers!.Clear();

        for (int i = 0; i < PaceManPlayers.Count; i++)
        {
            PaceManPlayers[i].Player = null;
        }

        PaceManPlayers.Clear();
        Configuration.ClearFromController();

        Task.Run(ObsController.Disconnect);

        return true;
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = PaceManPlayers };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceMan.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }

    private async void PaceManUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!_paceManWorker!.CancellationPending)
        {
            try
            {
                await RefreshPaceManAsync();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(Configuration.PaceManRefreshRateMiliseconds));
        }
    }
    private async Task RefreshPaceManAsync()
    {
        string result = await Helper.MakeRequestAsString(Consts.PaceManAPI);
        List<PaceMan>? paceMan = JsonSerializer.Deserialize<List<PaceMan>>(result);
        if (paceMan == null) return;

        List<PaceMan> notFoundPaceMans = new(PaceManPlayers);

        for (int i = 0; i < paceMan.Count; i++)
        {
            var resultPaceman = paceMan[i];
            bool foundPlayer = false;

            for (int j = 0; j < notFoundPaceMans.Count; j++)
            {
                var player = notFoundPaceMans[j];
                if (resultPaceman.Nickname.Equals(player.Nickname, StringComparison.OrdinalIgnoreCase))
                {
                    foundPlayer = true;
                    player.Update(resultPaceman);
                    notFoundPaceMans.Remove(player);
                    break;
                }
            }

            if (foundPlayer || resultPaceman.User.TwitchName == null) continue;

            int n = Configuration.Players.Count;
            for (int j = 0; j < n; j++)
            {
                var current = Configuration.Players[j];

                if (current.StreamData.ExistName(resultPaceman.User.TwitchName))
                {
                    resultPaceman.Player = current;
                }
            }

            //resultPaceman.Player = Configuration.Players.Where(x => x.TwitchName!.Equals(resultPaceman.User.TwitchName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (Configuration.IsUsingWhitelistOnPaceMan && resultPaceman.Player == null) continue;

            resultPaceman.Initialize(this, resultPaceman.Splits);
            AddPaceMan(resultPaceman);
        }

        for (int i = 0; i < notFoundPaceMans.Count; i++)
        {
            var current = notFoundPaceMans[i];
            RemovePaceMan(current);
        }

        OnPropertyChanged(nameof(PaceManPlayers));
        Application.Current.Dispatcher.Invoke(() => { GroupedPaceManPlayers?.Refresh(); });
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

    public void AddPaceMan(PaceMan paceMan)
    {
        Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Add(paceMan); });
    }
    public void RemovePaceMan(PaceMan paceMan)
    {
        Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Remove(paceMan); });
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

    private void SetPovAfterClickedCanvas()
    {
        if (CurrentChosenPOV == null || CurrentChosenPlayer == null) return;

        CurrentChosenPOV.SetPOV(CurrentChosenPlayer);
        UnSelectItems();
    }

    public void UnSelectItems(bool ClearAll = false)
    {
        CurrentChosenPlayer = null;
        _selectedPaceManPlayer = null;
        OnPropertyChanged(nameof(SelectedPaceManPlayer));
        _selectedWhitelistPlayer = null;
        OnPropertyChanged(nameof(SelectedWhitelistPlayer));
        if (ClearAll) CurrentChosenPOV = null;
    }

    public void GoBack()
    {
        MainViewModel.Open<PresetManagerViewModel>();
    }
}
