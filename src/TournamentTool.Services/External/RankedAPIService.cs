using System.Text.Json;
using TournamentTool.Domain.Entities;
using TournamentTool.Services.Logging;

namespace TournamentTool.Services.External;

public interface IRankedAPIService
{
    Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey);
}

public readonly record struct RankedErrorResponse(string status, string data);

public class RankedAPIService : IRankedAPIService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILoggingService _logger;

    private string _lastErrorResult = string.Empty;
    
    //API oparte o https://docs.mcsrranked.com/

    
    public RankedAPIService(IHttpClientFactory clientFactory, ILoggingService logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }
    
    public async Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,$"https://mcsrranked.com/api/users/{playerName}/live"); 
        request.Headers.Add("Private-Key", apiKey);

        var client = _clientFactory.CreateClient();
        using HttpResponseMessage response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string errorResult = await response.Content.ReadAsStringAsync();
            if (_lastErrorResult.Equals(errorResult)) return null;
            
            _lastErrorResult = errorResult;
            _logger.Error($"Ranked priv room data api error {response.StatusCode}: {errorResult}");
            return null;
        }

        await using Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(result);
    }
}