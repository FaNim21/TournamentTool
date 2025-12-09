using System.Text.Json.Serialization;

namespace TournamentTool.Domain.Entities;

public class PaceManEvent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_id")]
    public string? ID { get; set; }

    [JsonPropertyName("whitelist")]
    public string[]? WhiteList { get; set; }

    [JsonPropertyName("vanity")]
    public string VanityName { get; set; } = string.Empty;

    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
}

public struct PaceManTwitchResponse
{
    [JsonPropertyName("uuid")]
    public string UUID { get; init; }
    
    [JsonPropertyName("liveAccount")]
    public string Main { get; init; }

    [JsonPropertyName("alt")] 
    public string Alt { get; init; }
}
