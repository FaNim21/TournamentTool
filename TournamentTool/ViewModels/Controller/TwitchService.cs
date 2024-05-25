using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TournamentTool.Utils;
using System.Diagnostics;
using System.Windows;
using TournamentTool.Components.Controls;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;

namespace TournamentTool.ViewModels.Controller;

public class TwitchService
{
    private readonly TwitchAPI _api;


    public TwitchService()
    {
        _api = new TwitchAPI();
        _api.Settings.ClientId = Consts.ClientID;
    }

    public async Task AuthorizeAsync()
    {
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
}
