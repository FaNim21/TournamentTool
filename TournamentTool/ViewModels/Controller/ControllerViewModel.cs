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
    private readonly TwitchService _twitch;

    private readonly BackgroundWorker? _paceManWorker;
    private BackgroundWorker _twitchWorker = new();

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

    public MainViewModel MainViewModel { get; set; }
    public ObsController ObsController { get; set; }

    public Tournament Configuration { get => MainViewModel.CurrentChosen!; }

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

    public ICommand RefreshOBSCommand { get; set; }
    public ICommand RefreshPOVsCommand { get; set; }


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        ObsController = new(this);
        _twitch = new();

        RefreshOBSCommand = new RelayCommand(async () => { await ObsController.GetCurrentSceneitems(); });
        RefreshPOVsCommand = new RelayCommand(async () => { await RefreshPovs(); });

        /*Google.Apis.YouTube.v3.LiveStreamsResource.ListRequest
        Google.Apis.Auth.OAuth2.ExternalAccountCredential*/

        MainViewModel = mainViewModel;

        foreach (var player in MainViewModel.CurrentChosen!.Players)
            player.ShowCategory(!MainViewModel.CurrentChosen!.ShowLiveOnlyForMinecraftCategory);

        FilterItems();
        SetupPaceManGrouping();

        if (MainViewModel.CurrentChosen!.IsUsingPaceMan)
        {
            _paceManWorker = new() { WorkerSupportsCancellation = true };
            _paceManWorker.DoWork += PaceManUpdate;
            _paceManWorker.RunWorkerAsync();
        }

        Task.Factory.StartNew(async () =>
        {
            await ObsController.Connect(Configuration.Password!, Configuration.Port);
            await ConnectTwitchAPI();
        });
    }

    private void SetupPaceManGrouping()
    {
        var collectionViewSource = new CollectionViewSource { Source = PaceManPlayers };

        collectionViewSource.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PaceMan.SplitName)));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.SplitType), ListSortDirection.Descending));
        collectionViewSource.SortDescriptions.Add(new SortDescription(nameof(PaceMan.CurrentSplitTimeMiliseconds), ListSortDirection.Ascending));

        GroupedPaceManPlayers = collectionViewSource.View;
    }

    private async Task ConnectTwitchAPI()
    {
        _twitchWorker = new() { WorkerSupportsCancellation = true };
        if (!MainViewModel.CurrentChosen!.IsUsingTwitchAPI) return;

        await _twitch.AuthorizeAsync();

        _twitchWorker.DoWork += TwitchUpdate;
        _twitchWorker.RunWorkerAsync();
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
            await Task.Delay(TimeSpan.FromMilliseconds(MainViewModel.CurrentChosen!.PaceManRefreshRateMiliseconds));
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

            resultPaceman.Player = MainViewModel.CurrentChosen!.Players.Where(x => x.TwitchName!.Equals(resultPaceman.User.TwitchName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (MainViewModel.CurrentChosen!.IsUsingWhitelistOnPaceMan && resultPaceman.Player == null) continue;

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

    private async void TwitchUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!_twitchWorker.CancellationPending)
        {
            try
            {
                await UpdateTwitchInformations();
                FilterItems();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(30000));
        }
    }
    private async Task UpdateTwitchInformations()
    {
        List<string> logins = [];
        List<TwitchStreamData> notLivePlayers = [];
        for (int i = 0; i < MainViewModel.CurrentChosen!.Players.Count; i++)
        {
            var current = MainViewModel.CurrentChosen.Players[i];
            logins.Add(current.TwitchName!);
            notLivePlayers.Add(current.TwitchStreamData);
        }

        var streams = await _twitch.GetAllStreamsAsync(logins);
        for (int i = 0; i < streams.Count; i++)
        {
            var current = streams[i];
            if (!current.GameName.Equals("minecraft", StringComparison.OrdinalIgnoreCase) && MainViewModel.CurrentChosen.ShowLiveOnlyForMinecraftCategory) continue;

            for (int j = 0; j < notLivePlayers.Count; j++)
            {
                var twitch = notLivePlayers[j];
                if (!current.UserLogin.Equals(twitch.UserLogin, StringComparison.OrdinalIgnoreCase)) continue;

                TwitchStreamData stream = new()
                {
                    ID = current.Id,
                    BroadcasterID = current.UserId,
                    UserLogin = current.UserName,
                    GameName = current.GameName,
                    StartedAt = current.StartedAt,
                    Language = current.Language,
                    UserName = current.UserName,
                    Title = current.Title,
                    ThumbnailUrl = current.ThumbnailUrl,
                    ViewerCount = current.ViewerCount,
                    Status = current.Type,
                };

                twitch.Update(stream);
                notLivePlayers.Remove(twitch);
                j--;
            }
        }

        for (int i = 0; i < notLivePlayers.Count; i++)
        {
            var twitch = notLivePlayers[i];
            twitch.Clear();
        }
        notLivePlayers.Clear();
    }

    private void FilterItems()
    {
        //TODO: 0 przebudowac to tak zeby nie czyscic za kazdym razem
        if (MainViewModel.CurrentChosen == null) return;

        Application.Current.Dispatcher.Invoke(FilteredPlayers.Clear);

        IEnumerable<Player> playersToAdd = [];

        playersToAdd = MainViewModel.CurrentChosen.Players
                       .Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))
                       .OrderByDescending(player => player.TwitchStreamData.Status.Equals("live"));

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

    public void ControllerExit()
    {
        _paceManWorker?.CancelAsync();
        _paceManWorker?.Dispose();

        try
        {
            _twitchWorker?.CancelAsync();
        }
        catch { }
        _twitchWorker?.Dispose();

        for (int i = 0; i < MainViewModel.CurrentChosen!.Players.Count; i++)
        {
            var current = MainViewModel.CurrentChosen.Players[i];
            current.TwitchStreamData.StatusLabelColor = null;
        }

        POVs.Clear();
        FilteredPlayers!.Clear();

        for (int i = 0; i < PaceManPlayers.Count; i++)
        {
            PaceManPlayers[i].Player = null;
        }

        PaceManPlayers.Clear();

        for (int i = 0; i < Configuration.Players.Count; i++)
            Configuration.Players[i].ClearFromController();

        Task.Run(ObsController.Disconnect);
    }
}
