using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Utils;
using TwitchLib.Api;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : BaseViewModel
{
    public const string RedirectURL = "http://localhost:8080/redirect/";
    public const string PaceManAPI = "https://paceman.gg/api/ars/liveruns";

    private const float AspectRatio = 16.0f / 9.0f;

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

    public ObsClient? Client { get; set; } = new();
    public MainViewModel MainViewModel { get; set; }
    public TwitchAPI? TwitchAPI { get; set; }
    public HttpClient? WebServer { get; set; }

    public OBSVideoSettings OBSVideoSettings { get; set; } = new();

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

    private bool _isConnectedToWebSocket;
    public bool IsConnectedToWebSocket
    {
        get => _isConnectedToWebSocket;
        set
        {
            _isConnectedToWebSocket = value;
            OnPropertyChanged(nameof(IsConnectedToWebSocket));
        }
    }

    private string? _searchText;
    public string? SearchText
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
        private set
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
        private set
        {
            if (_canvasHeight == value) return;

            _canvasHeight = value;
            OnPropertyChanged(nameof(CanvasHeight));
            ResizeCanvas();
        }
    }

    public float CurrentPovVolume
    {
        get => CurrentChosenPOV!.Volume;
        set
        {
            if (CurrentChosenPOV!.Volume == value) return;
            CurrentChosenPOV.ChangeVolume(value);
            SetBrowserURL(CurrentChosenPOV);
            OnPropertyChanged(nameof(CurrentChosenPOV.Volume));
            OnPropertyChanged(nameof(CurrentChosenPOV));
        }
    }

    public float CanvasAspectRatio { get; private set; }

    public float XAxisRatio { get; private set; }
    public float YAxisRatio { get; private set; }

    public string CurrentSceneName { get; set; }

    public ICommand? ClipCommand { get; set; } = null;


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        TwitchAPI = new();
        TwitchAPI.Settings.ClientId = Consts.ClientID;

        ClipCommand = new RelayCommand(MakeClip);

        /*Google.Apis.YouTube.v3.LiveStreamsResource.ListRequest
        Google.Apis.Auth.OAuth2.ExternalAccountCredential*/

        MainViewModel = mainViewModel;
        FilterItems();

        if (MainViewModel.CurrentChosen!.IsUsingPaceMan)
        {
            paceManWorker = new() { WorkerSupportsCancellation = true };
            paceManWorker.DoWork += PaceManUpdate;
            paceManWorker.RunWorkerAsync();
        }

        Task.Run(async () =>
        {
            await ConnectToOBS();
            await TwitchApi();
        });
    }

    //TODO: 0 zrob wszystkie message boxy uzywajac to: MessageBoxOptions.DefaultDesktopOnly albo dodaj swoj dialog box nawet wyjdzie lepiej z tym
    private async Task ConnectToOBS()
    {
        if (Client == null || MainViewModel.CurrentChosen == null) return;

        bool isConnected = await Client.ConnectAsync(true, MainViewModel.CurrentChosen.Password!, "localhost", MainViewModel.CurrentChosen.Port, EventSubscriptions.All | EventSubscriptions.SceneItemTransformChanged);
        Client.ConnectionClosed += OnConnectionClosed;
        if (isConnected)
        {
            IsConnectedToWebSocket = true;
            try
            {
                while (Client.ConnectionState != ConnectionState.Connected)
                    await Task.Delay(100);

                await Client.SetCurrentSceneCollection(MainViewModel.CurrentChosen.SceneCollection!);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
                await Disconnect();
                return;
            }

            try
            {
                CanvasWidth = 426;
                CanvasHeight = 240;

                var settings = await Client.GetVideoSettings();
                OBSVideoSettings.BaseWidth = settings.BaseWidth;
                OBSVideoSettings.BaseHeight = settings.BaseHeight;
                OBSVideoSettings.OutputWidth = settings.OutputWidth;
                OBSVideoSettings.OutputHeight = settings.OutputHeight;
                OBSVideoSettings.AspectRatio = (float)settings.BaseWidth / settings.BaseHeight;

                XAxisRatio = settings.BaseWidth / CanvasWidth;
                OnPropertyChanged(nameof(XAxisRatio));
                YAxisRatio = settings.BaseHeight / CanvasHeight;
                OnPropertyChanged(nameof(YAxisRatio));

                CanvasAspectRatio = (float)CanvasWidth / CanvasHeight;
                OnPropertyChanged(nameof(CanvasAspectRatio));

                CurrentSceneName = await Client.GetCurrentProgramScene();
                OnPropertyChanged(nameof(CurrentSceneName));
                await GetCurrentSceneitems();

                Client.SceneItemCreated += OnSceneItemCreated;
                Client.SceneItemRemoved += OnSceneItemRemoved;
                Client.SceneItemTransformChanged += OnSceneItemTransformChanged;
                Client.CurrentProgramSceneChanged += OnCurrentProgramSceneChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
                await Disconnect();
                return;
            }
        }
    }

    public async Task Disconnect()
    {
        if (Client == null || !IsConnectedToWebSocket) return;

        Client.Disconnect();
        Client.Dispose();

        while (Client.ConnectionState != ConnectionState.Disconnected)
            await Task.Delay(100);

        Client.ConnectionClosed -= OnConnectionClosed;
        Client.SceneItemCreated -= OnSceneItemCreated;
        Client.SceneItemRemoved -= OnSceneItemRemoved;
        Client.SceneItemTransformChanged -= OnSceneItemTransformChanged;
        Client.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
    }

    public async Task GetCurrentSceneitems()
    {
        if (Client == null || string.IsNullOrEmpty(CurrentSceneName)) return;

        Application.Current.Dispatcher.Invoke(POVs.Clear);

        await Task.Delay(50);

        SceneItem[] sceneItems = await Client.GetSceneItemList(CurrentSceneName);
        foreach (var item in sceneItems)
        {
            if (!item.InputKind!.Equals("game_capture") &&
                !item.SourceName.StartsWith(MainViewModel.CurrentChosen!.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
                continue;

            PointOfView pov = new();
            SceneItemTransform transform = await Client.GetSceneItemTransform(CurrentSceneName, item.SceneItemId);

            pov.SceneName = CurrentSceneName;
            pov.SceneItemName = item.SourceName;
            pov.ID = item.SceneItemId;

            pov.X = (int)(transform.PositionX / XAxisRatio);
            pov.Y = (int)(transform.PositionY / YAxisRatio);

            pov.Width = (int)(transform.Width / XAxisRatio);
            pov.Height = (int)(transform.Height / YAxisRatio);

            pov.Text = item.SourceName;

            (string? currentName, float volume) = await GetBrowserURLTwitchName(pov.SceneItemName);
            pov.ChangeVolume(volume);

            if (!string.IsNullOrEmpty(currentName))
            {
                Player? player = MainViewModel.CurrentChosen!.GetPlayerByTwitchName(currentName);
                if (player != null)
                {
                    pov.TwitchName = currentName;
                    pov.DisplayedPlayer = player.Name!;
                    pov.Update();
                }
            }

            AddPov(pov);
        }
    }

    public void SetBrowserURL(PointOfView pov)
    {
        if (Client == null || pov == null || !IsConnectedToWebSocket) return;

        Dictionary<string, object> input = new() { { "url", pov.GetURL() }, };
        Client.SetInputSettings(pov.SceneItemName!, input);
    }
    public async Task<(string, float)> GetBrowserURLTwitchName(string sceneItemName)
    {
        if (Client == null || !IsConnectedToWebSocket) return (string.Empty, 0);

        var setting = await Client.GetInputSettings(sceneItemName);
        Dictionary<string, object> input = setting.InputSettings;

        string patternPlayerName = @"channel=([^&]+)";
        string patternVolume = @"volume=(\d+(\.\d+)?)";

        input.TryGetValue("url", out var address);
        if (address == null) return (string.Empty, 0);

        string url = address!.ToString()!;
        if (string.IsNullOrEmpty(url)) return (string.Empty, 0);

        string name = string.Empty;
        float volume = 0;

        Match matchName = Regex.Match(address!.ToString()!, patternPlayerName);
        Match matchVolume = Regex.Match(address!.ToString()!, patternVolume);

        if (matchName.Success)
        {
            name = matchName.Groups[1].Value;
        }
        if (matchVolume.Success)
        {
            if (float.TryParse(matchVolume.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float volumeValue))
            {
                volume = volumeValue;
            }
        }

        return (name, volume);
    }

    private async Task CreateNewSceneItem(string sceneName, string newSceneItemName, string inputKind)
    {
        if (Client == null || Client.ConnectionState == ConnectionState.Disconnected) return;

        Input input = new(inputKind, newSceneItemName, inputKind);
        await Client.CreateInput(sceneName, newSceneItemName, inputKind, input);
    }

    private async Task TwitchApi()
    {
        twitchWorker = new() { WorkerSupportsCancellation = true };
        if (TwitchAPI == null || !MainViewModel.CurrentChosen!.IsUsingTwitchAPI) return;

        var authScopes = new[] { TwitchLib.Api.Core.Enums.AuthScopes.Helix_Clips_Edit };
        string auth = TwitchAPI.Auth.GetAuthorizationCodeUrl(RedirectURL, authScopes, true, null, Consts.ClientID);

        try
        {
            var server = new WebServer(RedirectURL);

            Process.Start(new ProcessStartInfo
            {
                FileName = auth,
                UseShellExecute = true
            });

            var auth2 = await server.Listen();
            if (auth2 == null)
            {
                MessageBox.Show("Error with listening for twitch authentication");
                return;
            }
            var resp = await TwitchAPI.Auth.GetAccessTokenFromCodeAsync(auth2.Code, Consts.SecretID, RedirectURL, Consts.ClientID);
            TwitchAPI.Settings.AccessToken = resp.AccessToken;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
        }

        twitchWorker.DoWork += TwitchUpdate;
        twitchWorker.RunWorkerAsync();
    }
    private async Task UpdateTwitchInformations()
    {
        if (TwitchAPI == null || Client == null || Client.ConnectionState != ConnectionState.Connected) return;

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
            //if (!current.GameName.Equals("minecraft", StringComparison.OrdinalIgnoreCase)) continue;

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

    public void MakeClip()
    {
        if (TwitchAPI == null) return;

        Task.Run(Clip);
    }
    private async Task Clip()
    {
        try
        {
            var response = await TwitchAPI!.Helix.Clips.CreateClipAsync(MainViewModel.CurrentChosen!.Players[0].TwitchStreamData.BroadcasterID);
            string url = response.CreatedClips[0].EditUrl;
            Trace.WriteLine(url);
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}"); }
    }

    private void FilterItems()
    {
        if (MainViewModel.CurrentChosen == null) return;

        Application.Current.Dispatcher.Invoke(FilteredPlayers.Clear);

        IEnumerable<Player> playersToAdd = [];

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            playersToAdd = MainViewModel.CurrentChosen.Players;
        }
        else
        {
            playersToAdd = MainViewModel.CurrentChosen.Players
                           .Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
        }

        foreach (var player in playersToAdd)
            Application.Current.Dispatcher.Invoke(() => FilteredPlayers.Add(player));
    }
    private void OrderItemsByTwitchStatus()
    {
        //TODO: 0 pozniej zoptymalizowac filtrowanie przez zrobienie sturktury klas do konkretnego typu filtra czy cos
        if (MainViewModel.CurrentChosen == null || !string.IsNullOrWhiteSpace(SearchText)) return;

        Application.Current.Dispatcher.Invoke(FilteredPlayers.Clear);

        IEnumerable<Player> playersToAdd = [];
        playersToAdd = MainViewModel.CurrentChosen.Players
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
                OrderItemsByTwitchStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
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

        if (Client == null) return;
        Task.Run(Disconnect);
    }

    public void OnConnectionClosed(object? parametr, EventArgs args)
    {
        IsConnectedToWebSocket = false;
    }
    public void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs args)
    {
        if (!args.SourceName.StartsWith(MainViewModel.CurrentChosen!.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        SceneItemTransform transform = Client!.GetSceneItemTransform(args.SceneName, args.SceneItemId).Result;
        PointOfView pov = new()
        {
            SceneName = args.SceneName,
            SceneItemName = args.SourceName,
            ID = args.SceneItemId,
            X = (int)(transform.PositionX / XAxisRatio),
            Y = (int)(transform.PositionY / YAxisRatio),
            Width = (int)(transform.Width / XAxisRatio),
            Height = (int)(transform.Height / YAxisRatio),
            Text = args.SourceName
        };
        AddPov(pov);
        OnPropertyChanged(nameof(POVs));
    }
    public void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs args)
    {
        if (!args.SourceName.StartsWith(MainViewModel.CurrentChosen!.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase)) return;

        PointOfView? pov = null;
        for (int i = 0; i < POVs.Count; i++)
        {
            if (POVs[i].SceneItemName == args.SourceName)
            {
                pov = POVs[i];
                break;
            }
        }

        if (pov == null) return;
        RemovePov(pov);
    }
    public void OnSceneItemTransformChanged(object? parametr, SceneItemTransformChangedEventArgs args)
    {
        for (int i = 0; i < POVs.Count; i++)
        {
            var current = POVs[i];
            if (current.ID != args.SceneItemId) continue;

            current.X = (int)(args.SceneItemTransform.PositionX / XAxisRatio);
            current.Y = (int)(args.SceneItemTransform.PositionY / YAxisRatio);

            current.Width = (int)(args.SceneItemTransform.Width / XAxisRatio);
            current.Height = (int)(args.SceneItemTransform.Height / YAxisRatio);

            current.UpdateTransform();
        }
    }
    private void OnCurrentProgramSceneChanged(object? sender, SceneNameEventArgs e)
    {
        CurrentSceneName = e.SceneName;
        OnPropertyChanged(nameof(CurrentSceneName));
        Task.Run(GetCurrentSceneitems);
    }

    private void AddPov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate { POVs.Add(pov); });
    }
    private void RemovePov(PointOfView pov)
    {
        Application.Current.Dispatcher.Invoke(delegate { POVs.Remove(pov); });
    }

    public void ResizeCanvas()
    {
        if (CanvasWidth == 0 || CanvasHeight == 0) return;

        float calculatedHeight = CanvasWidth / AspectRatio;
        float calculatedWidth = CanvasHeight * AspectRatio;

        if (float.IsNaN(calculatedHeight) || float.IsInfinity(calculatedHeight) || float.IsNaN(calculatedWidth) || float.IsInfinity(calculatedWidth)) return;

        if (calculatedHeight > CanvasHeight)
            CanvasWidth = calculatedWidth;
        else
            CanvasHeight = calculatedHeight;
    }

    private async Task RefreshPaceManAsync()
    {
        string result = await Helper.MakeRequestAsString(PaceManAPI);
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
            catch (Exception ex) { MessageBox.Show(ex.Message + " - " + ex.StackTrace); }

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
        SetBrowserURL(CurrentChosenPOV);
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
