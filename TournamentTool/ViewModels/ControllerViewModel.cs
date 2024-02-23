using OBSStudioClient;
using OBSStudioClient.Classes;
using OBSStudioClient.Enums;
using OBSStudioClient.Events;
using OBSStudioClient.Messages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TournamentTool.Commands;
using TournamentTool.Models;
using TournamentTool.Utils;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;
using TwitchLib.Communication.Interfaces;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : BaseViewModel
{
    //TODO: 0 JAK BEDE DAWAC NA GITHUBA TO ZEBY TO UKRYC
    public const string PaceManAPI = "https://paceman.gg/api/ars/liveruns";
    public const string ClientID = "u10jjhgs6z6d7zi03pvt0d7vere72x";
    private const float AspectRatio = 16.0f / 9.0f;

    private readonly BackgroundWorker? worker;

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
    public TwitchAPI Api { get; set; } = new();

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


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        FilteredPlayers = new(MainViewModel.CurrentChosen!.Players);

        if (MainViewModel.CurrentChosen.IsUsingPaceMan)
        {
            worker = new() { WorkerSupportsCancellation = true };
            worker.DoWork += WorkerUpdate;
            worker.RunWorkerAsync();
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

    public void SetBrowserURL(string sceneItemName, string player)
    {
        if (Client == null || !IsConnectedToWebSocket) return;

        Dictionary<string, object> input = new() { { "url", $"https://player.twitch.tv/?channel={player}&enableExtensions=true&muted=false&parent=twitch.tv&player=popout&quality=chunked&volume=0" }, };
        Client.SetInputSettings(sceneItemName, input);
    }

    private async Task SetupSceneItem(string sceneName, string sceneItemName, int x, int y, int width, int height)
    {
        if (Client == null || Client.ConnectionState == ConnectionState.Disconnected) return;

        //await SetupSceneItem("Screen", "POV1", 150, 215, 800, 600);

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
        //Gets a list of all the subscritions of the specified channel.
        //var allSubscriptions = await Api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync("broadcasterID", null, 100, "accesstoken");

        //Get channels a specified user follows.
        //var userFollows = await Api.Helix.Users.GetUsersFollowsAsync("user_id");
        //var users = await Api.Helix.Users.getusers();
        //var user = users.Users;
        //await Api.Helix.Channels.GetChannelInformationAsync();

        //await Api.Helix.Streams.getstream

        //Get Specified Channel Follows
        //var channelFollowers = await Api.Helix.Users.GetUsersFollowsAsync(fromId: "channel_id");

        //Returns a stream object if online, if channel is offline it will be null/empty.
        //var streams = await Api.Helix.Streams.GetStreamsAsync(userIds: userIdsList); // Alternative: userLogins: userLoginsList

        //Update Channel Title/Game/Language/Delay - Only require 1 option here.
        //var request = new ModifyChannelInformationRequest() { GameId = "New_Game_Id", Title = "New stream title", BroadcasterLanguage = "New_Language", Delay = New_Delay };
        //await Api.Helix.Channels.ModifyChannelInformationAsync("broadcaster_Id", request, "AccessToken");


        /*string username = "lazy_boyenn";
        if (username != null)
        {
            string apiUrl = $"https://api.twitch.tv/helix/users?login={username}";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Client-ID", ClientID);

                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(responseData);
                    JToken user = json["data"]?.FirstOrDefault();

                    if (user != null)
                    {
                        string userId = user["id"]?.ToString();
                        if (!string.IsNullOrEmpty(userId))
                        {
                            // Now you can use the user ID to get stream information
                            string streamInfoUrl = $"https://api.twitch.tv/helix/streams?user_id={userId}";
                            HttpResponseMessage streamInfoResponse = await client.GetAsync(streamInfoUrl);

                            if (streamInfoResponse.IsSuccessStatusCode)
                            {
                                string streamInfoData = await streamInfoResponse.Content.ReadAsStringAsync();
                                JObject streamJson = JObject.Parse(streamInfoData);
                                JToken stream = streamJson["data"]?.FirstOrDefault();

                                if (stream != null)
                                {
                                    string title = stream["title"]?.ToString();
                                    int viewerCount = stream["viewer_count"]?.ToObject<int>() ?? 0;

                                    Trace.WriteLine($"Stream Title: {title}");
                                    Trace.WriteLine($"Viewer Count: {viewerCount}");
                                }
                                else
                                {
                                    Trace.WriteLine("User is not currently streaming.");
                                }
                            }
                        }
                    }
                    else
                    {
                        Trace.WriteLine("User not found.");
                    }
                }
            }
        }*/
    }

    private void FilterItems()
    {
        if (MainViewModel.CurrentChosen == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
            FilteredPlayers = new(MainViewModel.CurrentChosen.Players);
        else
            FilteredPlayers = new(MainViewModel.CurrentChosen.Players.Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
    }

    private async void WorkerUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!worker.CancellationPending)
        {
            await RefreshPaceManAsync();
            await Task.Delay(TimeSpan.FromMilliseconds(MainViewModel.CurrentChosen!.PaceManRefreshRateMiliseconds));
        }
    }

    public void ControllerExit()
    {
        worker?.CancelAsync();
        worker?.Dispose();

        POVs.Clear();
        PaceManPlayers.Clear();
        FilteredPlayers!.Clear();

        if (Client == null) return;
        Task.Run(Disconnect);
    }

    public void OnConnectionClosed(object? parametr, EventArgs args)
    {
        //MessageBox.Show("Lost connection");
        IsConnectedToWebSocket = false;
    }
    public void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs args)
    {
        if (!args.SourceName.StartsWith("pov", StringComparison.OrdinalIgnoreCase)) return;

        //TODO: 0 zbierac informacje czy pov jest typem browser
        //var info = MainViewModel.Client!.GetInputSettings(args.SourceName);

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
        if (!args.SourceName.StartsWith("pov", StringComparison.OrdinalIgnoreCase)) return;
        //TODO: 0 zbierac informacje czy pov jest typem browser

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
