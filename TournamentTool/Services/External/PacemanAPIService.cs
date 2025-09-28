using System.IO;
using System.Net.Http;
using System.Text.Json;
using TournamentTool.Models;

namespace TournamentTool.Services.External;

public interface IPacemanAPIService
{
    Task<PaceManEvent[]> GetPacemanEvents();
    Task<PaceManData[]> GetPacemanLiveData();
}

public class PacemanAPIService : IPacemanAPIService
{
    private readonly HttpClient _client;

    
    public PacemanAPIService(HttpClient client)
    {
        _client = client;
    }


    public async Task<PaceManEvent[]> GetPacemanEvents()
    {
        HttpResponseMessage response = await _client.GetAsync("https://paceman.gg/api/cs/eventlist");
        if (!response.IsSuccessStatusCode) return [];
            
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaceManEvent[]>(result) ?? [];
    }

    public async Task<PaceManData[]> GetPacemanLiveData()
    {
        HttpResponseMessage response = await _client.GetAsync("https://paceman.gg/api/ars/liveruns");
        if (!response.IsSuccessStatusCode) return [];
            
        Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PaceManData[]>(result) ?? [];
    }
}