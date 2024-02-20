using System.Text.Json.Serialization;

namespace TournamentTool.Commands;

public class PaceMan
{
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventLists")]
    public List<PaceEventList> Splits { get; set; } = [];
}

public class PaceEventList
{
    [JsonPropertyName("eventId")]
    public string? SplitName { get; set; }

    [JsonPropertyName("rta")]
    public long RTA { get; set; }

    [JsonPropertyName("igt")]
    public long IGT { get; set; }
}

public class PaceManUser
{
    [JsonPropertyName("uuid")]
    public string UUID { get; set; }

    [JsonPropertyName("liveAccount")]
    public string TwitchName { get; set; }
}
