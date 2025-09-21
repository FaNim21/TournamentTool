using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TournamentTool.Utils;
using System.Diagnostics;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Modules.Logging;
using TwitchLib.Api.Helix.Models.Clips.CreateClip;
using TournamentTool.Modules.OBS;
using TwitchLib.Api.Core.Exceptions;

namespace TournamentTool.Services;

public class TwitchService
{
    public ILoggingService Logger { get; }
    private ISettings SettingsService { get; }
    
    private readonly TwitchAPI _api;
    private const string ClientID = "1s5wfzaplbnwqtwnhhc29wav44c88y";

    public bool IsConnected { get; private set; }

    public DateTime? TokenExpiresAt { get; private set; }

    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    
    public TwitchService(ILoggingService logger, ISettings settingsService)
    {
        Logger = logger;
        SettingsService = settingsService;
        _api = new TwitchAPI { Settings = { ClientId = ClientID } };
    }

    public async Task ConnectAsync(bool silent = false)
    {
        _api.Settings.ClientId = !string.IsNullOrEmpty(SettingsService.APIKeys.CustomTwitchClientID) ? SettingsService.APIKeys.CustomTwitchClientID : ClientID;
        
        if (!string.IsNullOrEmpty(SettingsService.APIKeys.TwitchAccessToken))
        {
            _api.Settings.AccessToken = SettingsService.APIKeys.TwitchAccessToken;
        }
        
        if (!string.IsNullOrEmpty(_api.Settings.AccessToken) && IsConnected) return;
        if (!string.IsNullOrEmpty(_api.Settings.AccessToken) && !IsConnected)
        {
            try
            {
                var validateResponse = await _api.Auth.ValidateAccessTokenAsync(_api.Settings.AccessToken);
                if (validateResponse == null) throw new TokenExpiredException("Couldn't validate last token");
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(validateResponse.ExpiresIn);
                
                IsConnected = true;
                ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connecting, ConnectionState.Connected));
            
                Logger.Log("Successfully connected to Twitch API");
                return;
            }
            catch (TokenExpiredException ex)
            {
                Logger.Error(ex);
                SettingsService.APIKeys.TwitchAccessToken = string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SettingsService.APIKeys.TwitchAccessToken = string.Empty;
            }
        }
        
        var state = Guid.NewGuid().ToString();
        var authScopes = new[] { "clips:edit" };
        // string scopeParam = string.Join("+", authScopes);
        string scopeParam = string.Empty;
        
        string auth = $"https://id.twitch.tv/oauth2/authorize" +
                      $"?client_id={_api.Settings.ClientId}" +
                      $"&redirect_uri={Consts.RedirectURL}" +
                      $"&response_type=token" +
                      // $"&scope={scopeParam}" +
                      $"&state={state}";
        
        if (!silent)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Disconnected, ConnectionState.Connecting));
        }

        try
        {
            var server = new WebServer(Consts.RedirectURL, state);

            Process.Start(new ProcessStartInfo
            {
                FileName = auth,
                UseShellExecute = true
            });

            var authResponse = await server.Listen();
            if (authResponse == null)
            {
                Logger.Error("Error while listening for twitch authentication");
                return;
            }

            var validateResponse = await _api.Auth.ValidateAccessTokenAsync(authResponse.Code);
            _api.Settings.AccessToken = authResponse.Code;
            if (SettingsService.Settings.SaveTwitchToken)
            {
                SettingsService.APIKeys.TwitchAccessToken = authResponse.Code;
            }
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(validateResponse.ExpiresIn);
            
            IsConnected = true;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connecting, ConnectionState.Connected));
            
            Logger.Log("Successfully connected to Twitch API");
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
        TokenExpiresAt = null;
        IsConnected = false;
        
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(ConnectionState.Connected, ConnectionState.Disconnected));
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
        catch (InvalidCredentialException)
        {
            Logger.Error("Twitch API access token expired");
            Disconnect();
        }
        catch (BadScopeException ex)
        {
            Logger.Error(ex);
            Disconnect();
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
        catch (InvalidCredentialException)
        {
            Logger.Error("Twitch API access token expired");
            Disconnect();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error: {ex}");
        }
        return response;
    }
}