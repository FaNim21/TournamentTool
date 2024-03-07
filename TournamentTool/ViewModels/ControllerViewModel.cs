using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
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

    public float CanvasAspectRatio { get; private set; }

    public float XAxisRatio { get; private set; }
    public float YAxisRatio { get; private set; }

    public ICommand? ClipCommand { get; set; } = null;


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        TwitchAPI = new();
        TwitchAPI.Settings.ClientId = Consts.ClientID;

        ClipCommand = new RelayCommand(MakeClip);

        /*Google.Apis.YouTube.v3.LiveStreamsResource.ListRequest
        Google.Apis.Auth.OAuth2.ExternalAccountCredential*/

        MainViewModel = mainViewModel;
        FilteredPlayers = new(MainViewModel.CurrentChosen!.Players);

        if (MainViewModel.CurrentChosen.IsUsingPaceMan)
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
                await Client.SetCurrentProgramScene(MainViewModel.CurrentChosen.Scene!);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
                await Disconnect();
                return;
            }
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

            SceneItem[] sceneItems = await Client.GetSceneItemList(MainViewModel.CurrentChosen.Scene);
            foreach (var item in sceneItems)
            {
                if (item.SourceName.StartsWith(MainViewModel.CurrentChosen.FilterNameAtStartForSceneItems, StringComparison.OrdinalIgnoreCase))
                {
                    PointOfView pov = new();
                    SceneItemTransform transform = await Client.GetSceneItemTransform(MainViewModel.CurrentChosen.Scene, item.SceneItemId);

                    pov.SceneName = MainViewModel.CurrentChosen.Scene;
                    pov.SceneItemName = item.SourceName;
                    pov.ID = item.SceneItemId;

                    pov.X = (int)(transform.PositionX / XAxisRatio);
                    pov.Y = (int)(transform.PositionY / YAxisRatio);

                    pov.Width = (int)(transform.Width / XAxisRatio);
                    pov.Height = (int)(transform.Height / YAxisRatio);

                    pov.Text = item.SourceName;

                    AddPov(pov);
                    SetBrowserURL(pov.SceneItemName);
                }
            }

            Client.SceneItemCreated += OnSceneItemCreated;
            Client.SceneItemRemoved += OnSceneItemRemoved;
            Client.SceneItemTransformChanged += OnSceneItemTransformChanged;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
            await Disconnect();
            return;
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
    }

    public void SetBrowserURL(string sceneItemName, string? player = null)
    {
        if (Client == null || !IsConnectedToWebSocket) return;

        string url = $"https://player.twitch.tv/?channel={player}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0";
        if (string.IsNullOrEmpty(player)) url = "";
        Dictionary<string, object> input = new() { { "url", url }, };
        Client.SetInputSettings(sceneItemName, input);
    }

    private async Task SetupSceneItem(string sceneName, string sceneItemName, int x, int y, int width, int height)
    {
        if (Client == null || Client.ConnectionState == ConnectionState.Disconnected) return;

        Dictionary<string, object> input = new()
        {
            { "url", $"https://player.twitch.tv/?channel=zylenox&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0" },
            { "width", width},
            { "height", height},
        };
        await Client.SetInputSettings("POV1", input);

        SceneItemTransform transform = new(0, 0, 1, Bounds.OBS_BOUNDS_NONE, 1, 0, 0, 0, 0, 0, width / 2 + x, height / 2 + y, 0, 1, 1, 0, 0, 0);
        int id = await Client.GetSceneItemId(sceneName, sceneItemName);
        await Client.SetSceneItemTransform(sceneName, id, transform);
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
        if (TwitchAPI == null) return;

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
            if (!current.GameName.Equals("minecraft", StringComparison.OrdinalIgnoreCase)) continue;

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

        if (string.IsNullOrWhiteSpace(SearchText))
            FilteredPlayers = new(MainViewModel.CurrentChosen.Players);
        else
            FilteredPlayers = new(MainViewModel.CurrentChosen.Players.Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
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
            await UpdateTwitchInformations();
            await Task.Delay(TimeSpan.FromMilliseconds(15000));
        }
    }

    public void ControllerExit()
    {
        paceManWorker?.CancelAsync();
        paceManWorker?.Dispose();

        twitchWorker?.CancelAsync();
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
}
