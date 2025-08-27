using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TournamentTool.Utils;
using System.Diagnostics;
using System.Net;
using System.Timers;
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
    private int _expiresIn;

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    private System.Timers.Timer? _refreshTimer;
    
    
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

    private void ScheduleTokenRefresh(int expiresInSeconds)
    {
        StopRefreshingToken();

        int interval = Math.Max(1000, (expiresInSeconds - 600) * 1000); //Refreshing 10 min before expiring
        _refreshTimer = new System.Timers.Timer(interval);
        _refreshTimer.Elapsed += OnRefreshAccessTokenAsync;
        _refreshTimer.AutoReset = false;
        _refreshTimer.Start();
    }
    private void StopRefreshingToken()
    {
        if (_refreshTimer is null) return;
        
        _refreshTimer.Elapsed -= OnRefreshAccessTokenAsync;
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
    }

    public async Task ConnectAsync()
    {
        if (!string.IsNullOrEmpty(_refreshToken))
        {
            await RefreshAccessTokenAsync();
            return;
        }
        
        if (!string.IsNullOrEmpty(_api.Settings.AccessToken)) return;

        var state = Guid.NewGuid().ToString();
        var authScopes = new[] { TwitchLib.Api.Core.Enums.AuthScopes.Helix_Clips_Edit };
        string auth = _api.Auth.GetAuthorizationCodeUrl(Consts.RedirectURL, authScopes, true, state, _api.Settings.AccessToken);
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Connecting));

        try
        {
            var server = new WebServer(Consts.RedirectURL, state);

            Process.Start(new ProcessStartInfo
            {
                FileName = auth,
                UseShellExecute = true
            });

            var auth2 = await server.Listen();
            if (auth2 == null)
            {
                Logger.Error("Error while listening for twitch authentication");
                return;
            }

            var response = await _api.Auth.GetAccessTokenFromCodeAsync(auth2.Code, Consts.SecretID, Consts.RedirectURL, Consts.ClientID);
            _api.Settings.AccessToken = response.AccessToken;
            _expiresIn = response.ExpiresIn;
            _refreshToken = response.RefreshToken;
            IsConnected = true;
            ScheduleTokenRefresh(_expiresIn);
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connecting, ConnectionState.Connected));
        }
        catch (HttpListenerException ex) when (ex.ErrorCode == 995) 
        {
            Logger.Log("Web server listener stopped");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
            Disconnect();
        }
    }
    public void Disconnect()
    {
        _api.Settings.AccessToken = null;
        // _refreshToken = string.Empty;
        _expiresIn = 0;
        IsConnected = false;
        StopRefreshingToken();
        
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected));
    }
    
    private async void OnRefreshAccessTokenAsync(object? sender, ElapsedEventArgs e)
    {
        try
        {
            await RefreshAccessTokenAsync();
        }
        catch { /**/ }
    }
    private async Task RefreshAccessTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken)) return;
        
        try
        {
            var response = await _api.Auth.RefreshAuthTokenAsync(_refreshToken, Consts.SecretID, Consts.ClientID);
            _api.Settings.AccessToken = response.AccessToken;
            _expiresIn = response.ExpiresIn;
            _refreshToken = response.RefreshToken;
            IsConnected = true;
            ScheduleTokenRefresh(_expiresIn);
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connecting, ConnectionState.Connected));
        }
        catch (Exception ex)
        {
            Logger.Error($"Error refreshing twitch access token: {ex}");
            Disconnect();
            return;
        }
        
        Logger.Log($"Successfully refreshed Access token");
    }
    
    public async Task<List<Stream>> GetAllStreamsAsync(List<string> logins)
    {
        var allStreams = new List<Stream>();
        const int batchSize = 100;

        var loginBatches = logins
            .Select((login, index) => new { login, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.login).ToList())
            .ToList();
        
        try
        {
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
            Logger.Error("Twitch API access token expired");
        }
        catch (BadRequestException ex)
        {
            Logger.Error($"Error requesting streams: {ex}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error while fetching streaming datas - {ex}");
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
            Logger.Error($"Error: {ex}");
        }
        return response;
    }
}