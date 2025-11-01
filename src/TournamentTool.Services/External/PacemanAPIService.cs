using System.Text.Json;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.External;

public interface IPacemanAPIService
{
    Task<PaceManEvent[]> GetPacemanEvents();
    Task<PaceManData[]> GetPacemanLiveData();
}

public class PacemanAPIService : IPacemanAPIService
{
    private readonly IHttpClientFactory _clientFactory;


    public PacemanAPIService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<PaceManEvent[]> GetPacemanEvents()
    {
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("https://paceman.gg/api/cs/eventlist");
        if (!response.IsSuccessStatusCode) return [];
            
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaceManEvent[]>(result) ?? [];
    }

    public async Task<PaceManData[]> GetPacemanLiveData()
    {
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("https://paceman.gg/api/ars/liveruns");
        if (!response.IsSuccessStatusCode) return [];
            
        Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PaceManData[]>(result) ?? [];
    }
}