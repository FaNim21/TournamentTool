using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TournamentTool.Utils;
using System.Diagnostics;
using System.Windows;
using TournamentTool.Components.Controls;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;
using System.ComponentModel;
using System.Configuration;
using TournamentTool.Models;

namespace TournamentTool.ViewModels.Controller;

public class TwitchService
{
    private ControllerViewModel Controller { get; set; }

    private readonly TwitchAPI _api;

    private BackgroundWorker? _twitchWorker;


    public TwitchService(ControllerViewModel controllerViewModel)
    {
        Controller = controllerViewModel;

        _api = new TwitchAPI();
        _api.Settings.ClientId = Consts.ClientID;
    }

    public async Task AuthorizeAsync()
    {
        if (!string.IsNullOrEmpty(_api.Settings.AccessToken)) return;

        var authScopes = new[] { TwitchLib.Api.Core.Enums.AuthScopes.Helix_Clips_Edit };
        string auth = _api.Auth.GetAuthorizationCodeUrl(Consts.RedirectURL, authScopes, true, null, _api.Settings.AccessToken);

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
            var resp = await _api.Auth.GetAccessTokenFromCodeAsync(auth2.Code, Consts.SecretID, Consts.RedirectURL, Consts.ClientID);
            _api.Settings.AccessToken = resp.AccessToken;
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task<List<Stream>> GetAllStreamsAsync(List<string> logins)
    {
        var allStreams = new List<Stream>();
        string cursor = null;

        try
        {
            do
            {
                var streamsResponse = await _api.Helix.Streams.GetStreamsAsync(userLogins: logins, after: cursor);
                allStreams.AddRange(streamsResponse.Streams);
                cursor = streamsResponse.Pagination.Cursor;
            } while (!string.IsNullOrEmpty(cursor));
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return allStreams.DistinctBy(stream => stream.UserId).ToList();
    }

    public async Task<CreatedClipResponse> CreateClipAsync(string broadcasterID)
    {
        CreatedClipResponse response = null;
        try
        {

            response = await _api.Helix.Clips.CreateClipAsync(broadcasterID);
            //string url = response.CreatedClips[0].EditUrl;
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return response;
    }

    public async Task ConnectTwitchAPIAsync() 
    {
        _twitchWorker = new() { WorkerSupportsCancellation = true };
        if (!Controller.Configuration.IsUsingTwitchAPI) return;

        await AuthorizeAsync();

        _twitchWorker.DoWork += TwitchUpdate;
        _twitchWorker.RunWorkerAsync();
    }

    private async void TwitchUpdate(object? sender, DoWorkEventArgs e)
    {
        while (!_twitchWorker!.CancellationPending)
        {
            try
            {
                await UpdateTwitchInformations();
                Controller.FilterItems();
            }
            catch (Exception ex)
            {
                DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(60000));
        }
    }
    private async Task UpdateTwitchInformations()
    {
        List<string> logins = [];
        List<StreamData> notLivePlayers = [];

        for (int i = 0; i < Controller.Configuration.Players.Count; i++)
        {
            var current = Controller.Configuration.Players[i];
            current.StreamData.LiveData.WasUpdated = false;

            if (!string.IsNullOrEmpty(current.StreamData.Main))
                logins.Add(current.StreamData.Main!);
            if (!string.IsNullOrEmpty(current.StreamData.Alt))
                logins.Add(current.StreamData.Alt!);

            notLivePlayers.Add(current.StreamData);
        }

        var streams = await GetAllStreamsAsync(logins);
        for (int i = 0; i < streams.Count; i++)
        {
            var current = streams[i];
            if (!current.GameName.Equals("minecraft", StringComparison.OrdinalIgnoreCase) && Controller.Configuration.ShowLiveOnlyForMinecraftCategory) continue;

            for (int j = 0; j < notLivePlayers.Count; j++)
            {
                var streamData = notLivePlayers[j];
                if (!streamData.ExistName(current.UserLogin)) continue;

                bool isMainStream = current.UserLogin.Equals(streamData.Main, StringComparison.OrdinalIgnoreCase);
                bool isAltStream = current.UserLogin.Equals(streamData.Alt, StringComparison.OrdinalIgnoreCase);

                TwitchStreamData liveData = new()
                {
                    ID = current.Id,
                    BroadcasterID = current.UserId,
                    UserLogin = current.UserLogin,
                    GameName = current.GameName,
                    StartedAt = current.StartedAt,
                    Language = current.Language,
                    UserName = current.UserName,
                    Title = current.Title,
                    ThumbnailUrl = current.ThumbnailUrl,
                    ViewerCount = current.ViewerCount,
                    Status = current.Type,
                };

                if (isMainStream)
                {
                    streamData.LiveData.Update(liveData);
                    notLivePlayers.RemoveAt(j);
                    j--;
                }
                else if (isAltStream)
                {
                    streamData.LiveData.Update(liveData);
                }
            }
        }

        for (int i = 0; i < notLivePlayers.Count; i++)
        {
            var toClear = notLivePlayers[i];
            if (toClear.LiveData.WasUpdated) continue;

            toClear.LiveData.Clear();
        }
        notLivePlayers.Clear();
    }

    public void Dispose()
    {
        try
        {
            _twitchWorker?.CancelAsync();
        }
        catch { }
        _twitchWorker?.Dispose();
    }
}
