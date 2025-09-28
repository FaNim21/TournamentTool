using System.IO;
using System.Net.Http;
using System.Text.Json;
using TournamentTool.Extensions;
using TournamentTool.Models;

namespace TournamentTool.Services.External;

public interface IMinecraftDataService
{
    Task<byte[]> GetPlayerHeadAsync(string id, int size);
    Task<ResponseMojangProfileAPI?> GetDataFromUUID(string UUID);
    Task<ResponseMojangProfileAPI?> GetDataFromIGN(string inGameName);
}

public class MinecraftDataService : IMinecraftDataService
{
    private readonly HttpClient _client;
    private readonly SettingsService _settingsService;


    public MinecraftDataService(HttpClient client, SettingsService settingsService)
    {
        _client = client;
        _settingsService = settingsService;
    }

    public async Task<byte[]> GetPlayerHeadAsync(string id, int size)
    {
        string url = _settingsService.Settings.HeadAPIType.GetHeadURL(id, size);
        try
        {
            HttpResponseMessage response = await _client.GetAsync(url);
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
            
        HttpResponseMessage response = await _client.GetAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{UUID}");
        if (!response.IsSuccessStatusCode) return null;
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
    public async Task<ResponseMojangProfileAPI?> GetDataFromIGN(string inGameName)
    {
        if (string.IsNullOrEmpty(inGameName)) return null;
            
        HttpResponseMessage response = await _client.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{inGameName}");
        if (!response.IsSuccessStatusCode) return null;
        
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResponseMojangProfileAPI>(result);
    }
}