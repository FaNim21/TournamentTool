using System.Text.Json;
using TournamentTool.Core.Extensions;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.External;

public interface IMinecraftDataService
{
    Task<byte[]> GetPlayerHeadAsync(string id, int size);
    Task<ResponseMojangProfileAPI?> GetDataFromUUID(string UUID);
    Task<ResponseMojangProfileAPI?> GetDataFromIGN(string inGameName);
}

public class MinecraftDataService : IMinecraftDataService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILoggingService _logger;
    
    private readonly Settings _settings;


    public MinecraftDataService(IHttpClientFactory clientFactory, ISettingsProvider settingsProvider, ILoggingService logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        
        _settings = settingsProvider.Get<Settings>();
    }

    public async Task<byte[]> GetPlayerHeadAsync(string id, int size)
    {
        string url = _settings.HeadAPIType.GetHeadURL(id, size);
        try
        {
            var client = _clientFactory.CreateClient();
            HttpResponseMessage response = await client.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorResult = await response.Content.ReadAsStringAsync();
                _logger.Error($"{_settings.HeadAPIType.ToString()} api error {response.StatusCode}: {errorResult}");
                return [];
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch
        {
            return [];
        }
    }

    public async Task<ResponseMojangProfileAPI?> GetDataFromUUID(string UUID)
    {
        if (string.IsNullOrEmpty(UUID)) return null;
            
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{UUID}");
        if (!response.IsSuccessStatusCode)
        {
            string errorResult = await response.Content.ReadAsStringAsync();
            _logger.Error($"Mojang sessionserver profiles api error{response.StatusCode}: {errorResult}");
            return null;
        }
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
    public async Task<ResponseMojangProfileAPI?> GetDataFromIGN(string inGameName)
    {
        if (string.IsNullOrEmpty(inGameName)) return null;
            
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{inGameName}");
        if (!response.IsSuccessStatusCode)
        {
            string errorResult = await response.Content.ReadAsStringAsync();
            _logger.Error($"Mojang profiles api error {response.StatusCode}: {errorResult}");
            return null;
        }
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
}