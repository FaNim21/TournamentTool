using System.Text.Json;
using TournamentTool.Core.Extensions;
using TournamentTool.Domain.Entities;

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
    private readonly SettingsService _settingsService;


    public MinecraftDataService(IHttpClientFactory clientFactory, SettingsService settingsService)
    {
        _clientFactory = clientFactory;
        _settingsService = settingsService;
    }

    public async Task<byte[]> GetPlayerHeadAsync(string id, int size)
    {
        string url = _settingsService.Settings.HeadAPIType.GetHeadURL(id, size);
        try
        {
            var client = _clientFactory.CreateClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return [];

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
        if (!response.IsSuccessStatusCode) return null;
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
    public async Task<ResponseMojangProfileAPI?> GetDataFromIGN(string inGameName)
    {
        if (string.IsNullOrEmpty(inGameName)) return null;
            
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{inGameName}");
        if (!response.IsSuccessStatusCode) return null;
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
}