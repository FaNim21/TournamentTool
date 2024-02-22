using System.Text.Json.Serialization;

namespace TournamentTool.Models;

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
    public string uuid { get; set; }
    public string liveAccount { get; set; }
}
