using System.Text.Json;
using TournamentTool.Domain.Entities;

namespace TournamentTool.Services.External;

public interface IRankedAPIService
{
    Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey);
}

public class RankedAPIService : IRankedAPIService
{
    private readonly IHttpClientFactory _clientFactory;
    //API oparte o https://docs.mcsrranked.com/
    

    
    public RankedAPIService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    
    public async Task<PrivRoomAPIResult?> GetRankedPrivateRoomLiveData(string playerName, string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,$"https://mcsrranked.com/api/users/{playerName}/live"); 
        request.Headers.Add("Private-Key", apiKey);

        var client = _clientFactory.CreateClient();
        using HttpResponseMessage response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        await using Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(result);}
}