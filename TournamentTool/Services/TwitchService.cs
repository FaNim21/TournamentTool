using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TournamentTool.Utils;
using System.Diagnostics;
using System.Windows;
using TournamentTool.Components.Controls;
using TournamentTool.Modules.Logging;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;
using TournamentTool.Modules.OBS;
using TwitchLib.Api.Core.Exceptions;

namespace TournamentTool.Services;

public class TwitchService
{
    public ILoggingService Logger { get; }
    private readonly TwitchAPI _api;

    public bool IsConnected { get; private set; }

    private string _refreshToken = string.Empty;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    public TwitchService(ILoggingService logger)
    {
        Logger = logger;
        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = Consts.ClientID
            }
        };
    }

    public async Task ConnectAsync()
    {
        if (!string.IsNullOrEmpty(_api.Settings.AccessToken)) return;

        var authScopes = new[] { TwitchLib.Api.Core.Enums.AuthScopes.Helix_Clips_Edit };
        string auth = _api.Auth.GetAuthorizationCodeUrl(Consts.RedirectURL, authScopes, true, null, _api.Settings.AccessToken);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Connecting));

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

            var response = await _api.Auth.GetAccessTokenFromCodeAsync(auth2.Code, Consts.SecretID, Consts.RedirectURL, Consts.ClientID);
            _api.Settings.AccessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
            IsConnected = true;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connecting, ConnectionState.Connected));
        }
        catch (Exception ex)
        {
            DialogBox.Show($"Error: {ex.Message} - {ex.StackTrace}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            Disconnect();
        }
    }
    public void Disconnect()
    {
        _api.Settings.AccessToken = null;
        _refreshToken = string.Empty;
        IsConnected = false;
        
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected));
    }
    
    public async Task RefreshAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken)) return;
        
        var oldToken = _refreshToken;
        try
        {
            var response = await _api.Auth.RefreshAuthTokenAsync(_refreshToken, Consts.SecretID, Consts.ClientID);
            _api.Settings.AccessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
        }
        catch (Exception)
        {
            Disconnect();
        }
        
        Logger.Log($"Refreshing AccessToken, old: {oldToken}, new: {_refreshToken}");
    }
    
    public async Task<List<Stream>> GetAllStreamsAsync(List<string> logins)
    {
        var allStreams = new List<Stream>();
        const int batchSize = 100;

        try
        {
            var loginBatches = logins
                .Select((login, index) => new { login, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.login).ToList())
                .ToList();

            foreach (var batch in loginBatches)
            {
                var cursor = string.Empty;

                do
                {
                    var streamsResponse = await _api.Helix.Streams.GetStreamsAsync(userLogins: batch, after: cursor);
                    allStreams.AddRange(streamsResponse.Streams);
                    cursor = streamsResponse.Pagination.Cursor;
                } while (!string.IsNullOrEmpty(cursor));
            }
        }
        catch (TokenExpiredException)
        {
            await RefreshAccessTokenAsync();
            await GetAllStreamsAsync(logins);
        }
        catch (BadRequestException)
        {
            Logger.Error("Error requesting streams - probably wrong twitch name");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error while fetching streaming datas - {ex.Message}\n{ex.StackTrace}");
        }

        return allStreams.DistinctBy(stream => stream.UserId).ToList();
    }
    public async Task<CreatedClipResponse?> CreateClipAsync(string broadcasterID)
    {
        CreatedClipResponse? response = null;
        try
        {
            response = await _api.Helix.Clips.CreateClipAsync(broadcasterID);
            // string url = response.CreatedClips[0].EditUrl;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex.Message} - {ex.StackTrace}");
        }
        return response;
    }
}