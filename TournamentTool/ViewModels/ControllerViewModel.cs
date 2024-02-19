using Newtonsoft.Json.Linq;
using OBSStudioClient.Classes;
using OBSStudioClient.Events;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Models;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation;

namespace TournamentTool.ViewModels;

public class ControllerViewModel : BaseViewModel
{
    public ObservableCollection<PointOfView> POVs { get; set; } = [];

    public TwitchAPI Api { get; set; } = new();

    //TODO: 0 JAK BEDE DAWAC NA GITHUBA TO ZEBY TO UKRYC
    public const string ClientID = "u10jjhgs6z6d7zi03pvt0d7vere72x";

    public MainViewModel MainViewModel { get; set; }

    private ObservableCollection<Player>? _filteredPlayers;
    public ObservableCollection<Player>? FilteredPlayers
    {
        get => _filteredPlayers;
        set
        {
            _filteredPlayers = value;
            OnPropertyChanged(nameof(FilteredPlayers));
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

    private const int canvasWidth = 426;
    private const int canvasHeight = 240;


    public ControllerViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        FilteredPlayers = MainViewModel.CurrentChosen!.Players;

        Task.Run(async () =>
        {
            await LoadSceneData();
            await TwitchApi();
        });
    }

    private async Task LoadSceneData()
    {
        if (MainViewModel.Client == null || MainViewModel.CurrentChosen == null) return;

        try
        {
            Application.Current.Dispatcher.Invoke(MainViewModel.CurrentChosen.POVs.Clear);

            var settings = await MainViewModel.Client.GetVideoSettings();
            float xAxisRatio = settings.BaseWidth / canvasWidth;
            float yAxisRatio = settings.BaseHeight / canvasHeight;

            SceneItem[] sceneItems = await MainViewModel.Client.GetSceneItemList(MainViewModel.CurrentChosen.Scene);
            foreach (var item in sceneItems)
            {
                if (item.SourceName.StartsWith("pov", StringComparison.OrdinalIgnoreCase))
                {
                    PointOfView pov = new();
                    SceneItemTransform transform = await MainViewModel.Client.GetSceneItemTransform(MainViewModel.CurrentChosen.Scene, item.SceneItemId);

                    pov.SceneName = MainViewModel.CurrentChosen.Scene;
                    pov.SceneItemName = item.SourceName;

                    pov.X = (int)(transform.PositionX / xAxisRatio);
                    pov.Y = (int)(transform.PositionY / yAxisRatio);

                    pov.Width = (int)(transform.Width / xAxisRatio);
                    pov.Height = (int)(transform.Height / yAxisRatio);

                    pov.Text = item.SourceName;

                    Application.Current.Dispatcher.Invoke(delegate { MainViewModel.AddPOV(pov); });
                }
            }

            MainViewModel.Client.SceneItemCreated += OnSceneItemCreated;
            MainViewModel.Client.SceneItemRemoved += OnSceneItemRemoved;
            MainViewModel.Client.SceneItemTransformChanged += OnSceneItemTransformChanged;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message} - {ex.StackTrace}");
            MainViewModel.Disconnect();
            return;
        }
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
            FilteredPlayers = MainViewModel.CurrentChosen.Players;
        else
            FilteredPlayers = new ObservableCollection<Player>(MainViewModel.CurrentChosen.Players.Where(player => player.Name!.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
    }

    public void ControllerExit()
    {
        if (MainViewModel.Client == null) return;

        MainViewModel.Client.SceneItemCreated -= OnSceneItemCreated;
        MainViewModel.Client.SceneItemRemoved -= OnSceneItemRemoved;
        MainViewModel.Client.SceneItemTransformChanged -= OnSceneItemTransformChanged;
    }

    public void OnSceneItemCreated(object? parametr, SceneItemCreatedEventArgs args)
    {

    }

    public void OnSceneItemRemoved(object? parametr, SceneItemRemovedEventArgs args)
    {

    }

    public void OnSceneItemTransformChanged(object? parametr, SceneItemTransformChangedEventArgs args)
    {
        
    }
}
