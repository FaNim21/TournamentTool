using OBSStudioClient.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using TournamentTool.Commands;
using TournamentTool.Components.Controls;
using TournamentTool.Models;
using TournamentTool.Utils;
using TwitchLib.Api;

namespace TournamentTool.ViewModels.Controller;

public class ControllerViewModel : BaseViewModel
{
    private readonly BackgroundWorker? paceManWorker;
    private BackgroundWorker twitchWorker = new();

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

    public MainViewModel MainViewModel { get; set; }
    public ObsController ObsController { get; set; }

    public Tournament Configuration { get => MainViewModel.CurrentChosen!; }

    public TwitchAPI? TwitchAPI { get; set; }
    public HttpClient? WebServer { get; set; }

    private ITwitchPovInformation? _currentChosenPlayer;
    public ITwitchPovInformation? CurrentChosenPlayer
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


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        TwitchAPI = new();
        ObsController = new(this);

        TwitchAPI.Settings.ClientId = Consts.ClientID;

        /*Google.Apis.YouTube.v3.LiveStreamsResource.ListRequest
        Google.Apis.Auth.OAuth2.ExternalAccountCredential*/

        MainViewModel = mainViewModel;

        foreach (var player in MainViewModel.CurrentChosen!.Players)
            player.ShowCategory(!MainViewModel.CurrentChosen!.ShowLiveOnlyForMinecraftCategory);

        FilterItems();

        if (MainViewModel.CurrentChosen!.IsUsingPaceMan)
        {
            paceManWorker = new() { WorkerSupportsCancellation = true };
            paceManWorker.DoWork += PaceManUpdate;
            paceManWorker.RunWorkerAsync();
        }

        Task.Run(async () =>
        {
            await ObsController.Connect(Configuration.Password!, Configuration.Port);
            await TwitchApi();
        });
    }

    private async Task TwitchApi()
    {
        twitchWorker = new() { WorkerSupportsCancellation = true };
        if (TwitchAPI == null || !MainViewModel.CurrentChosen!.IsUsingTwitchAPI) return;

        var authScopes = new[] { TwitchLib.Api.Core.Enums.AuthScopes.Helix_Clips_Edit };
        string auth = TwitchAPI.Auth.GetAuthorizationCodeUrl(Consts.RedirectURL, authScopes, true, null, Consts.ClientID);

        try
        {
            var server = new WebServer(Consts.RedirectURL);

            Process.Start(new ProcessStartInfo
            {
                FileName = auth,
                UseShellExecute = true
            });

            var auth2 = await server.Listen();
            if (auth2 == null)
            {
                DialogBox.Show($"Error with listening for twitch authentication", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var resp = await TwitchAPI.Auth.GetAccessTokenFromCodeAsync(auth2.Code, Consts.SecretID, Consts.RedirectURL, Consts.ClientID);
            TwitchAPI.Settings.AccessToken = resp.AccessToken;
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        twitchWorker.DoWork += TwitchUpdate;
        twitchWorker.RunWorkerAsync();
    }
    private async Task UpdateTwitchInformations()
    {
        if (TwitchAPI == null || ObsController.Client == null || ObsController.Client.ConnectionState != ConnectionState.Connected) return;

        List<string> logins = [];
        for (int i = 0; i < MainViewModel.CurrentChosen!.Players.Count; i++)
        {
            var current = MainViewModel.CurrentChosen.Players[i];
            logins.Add(current.TwitchName!);
        }

        var response = await TwitchAPI.Helix.Streams.GetStreamsAsync(userLogins: logins);
        List<TwitchStreamData> notLivePlayers = [];
        foreach (var player in MainViewModel.CurrentChosen.Players)
            notLivePlayers.Add(player.TwitchStreamData);

        for (int i = 0; i < response.Streams.Length; i++)
        {
            var current = response.Streams[i];
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

    private async void PaceManUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!paceManWorker.CancellationPending)
        {
            await RefreshPaceManAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(MainViewModel.CurrentChosen!.PaceManRefreshRateMiliseconds));
        }
    }
    private async void TwitchUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!twitchWorker.CancellationPending)
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
            await Task.Delay(TimeSpan.FromMilliseconds(15000));
        }
    }

    public void ControllerExit()
    {
        paceManWorker?.CancelAsync();
        paceManWorker?.Dispose();

        try
        {
            twitchWorker?.CancelAsync();
        }
        catch { }
        twitchWorker?.Dispose();

        for (int i = 0; i < MainViewModel.CurrentChosen!.Players.Count; i++)
        {
            var current = MainViewModel.CurrentChosen.Players[i];
            current.TwitchStreamData.StatusLabelColor = null;
        }

        POVs.Clear();
        PaceManPlayers.Clear();
        FilteredPlayers!.Clear();

        if (ObsController.Client == null) return;
        Task.Run(ObsController.Disconnect);
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

            if (foundPlayer) continue;
            if (resultPaceman.User.TwitchName == null) continue;

            try
            {
                resultPaceman.Player = MainViewModel.CurrentChosen!.Players.Where(x => x.TwitchName == resultPaceman.User.TwitchName).FirstOrDefault();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (MainViewModel.CurrentChosen!.IsUsingWhitelistOnPaceMan && resultPaceman.Player == null) continue;
            string name = Helper.GetSplitShortcut(resultPaceman.Splits.Last().SplitName);
            resultPaceman.UpdateTime(name);
            Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Add(resultPaceman); });
        }

        for (int i = 0; i < notFoundPaceMans.Count; i++)
        {
            var current = notFoundPaceMans[i];
            Application.Current.Dispatcher.Invoke(() => { PaceManPlayers.Remove(current); });
        }
    }

    private void SetPovAfterClickedCanvas()
    {
        if (CurrentChosenPOV == null || CurrentChosenPlayer == null) return;

        CurrentChosenPOV.DisplayedPlayer = CurrentChosenPlayer!.GetDisplayName();
        CurrentChosenPOV.TwitchName = CurrentChosenPlayer!.GetTwitchName();
        CurrentChosenPOV.Update();
        ObsController.SetBrowserURL(CurrentChosenPOV);
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
}
