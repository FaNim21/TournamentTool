using System.Text.Json;
using TournamentTool.Domain.Entities;

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
        var request = new HttpRequestMessage(HttpMethod.Get,$"https://mcsrranked.com/api/users/{playerName}/live"); 
        request.Headers.Add("Private-Key", apiKey);
        
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        await using Stream result = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<PrivRoomAPIResult>(result);}
}