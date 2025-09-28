using System.IO;
using System.Net.Http;
using System.Text.Json;
using TournamentTool.Models;

namespace TournamentTool.Services.External;

public interface IRankedAPIService
{
    Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey);
}

public class RankedAPIService : IRankedAPIService
{
    //API oparte o https://docs.mcsrranked.com/
    
    private readonly HttpClient _client;

    
    public RankedAPIService(HttpClient client)
    {
        _client = client;
    }
    
    public async Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey)
    {
        HttpResponseMessage response = await _client.GetAsync($"https://mcsrranked.com/api/users/{playerName}/live");
        _client.DefaultRequestHeaders.Add("Private-Key", apiKey);
        if (!response.IsSuccessStatusCode) return null;
            
        Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(result);
    }
}