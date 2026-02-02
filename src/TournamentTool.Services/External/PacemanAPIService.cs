using System.Text.Json;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.External;

public interface IPacemanAPIService
{
    Task<PaceManEvent[]> GetPacemanEvents();
    Task<PaceManData[]?> GetPacemanLiveData();
}

public class PacemanAPIService : IPacemanAPIService
{
    private readonly IHttpClientFactory _clientFactory;

    private readonly ILoggingService _logger;
    //in future caching paceman events etc so no need to have playermanager as singleton by this


    public PacemanAPIService(IHttpClientFactory clientFactory, ILoggingService logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<PaceManEvent[]> GetPacemanEvents()
    {
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(Consts.PacemanEventListAPI);
        if (!response.IsSuccessStatusCode)
        {
            string errorResult = await response.Content.ReadAsStringAsync();
            _logger.Error($"Paceman events api error: {response.StatusCode}: {errorResult}");
            return [];
        }
            
        string result = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaceManEvent[]>(result) ?? [];
    }

    public async Task<PaceManData[]?> GetPacemanLiveData()
    {
        var client = _clientFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync(Consts.PacemanAPI);
        if (!response.IsSuccessStatusCode)
        {
            string errorResult = await response.Content.ReadAsStringAsync();
            _logger.Error($"Paceman live data api error: {response.StatusCode}: {errorResult}");
            return null;
        }
            
        Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PaceManData[]>(result) ?? [];
    }
}