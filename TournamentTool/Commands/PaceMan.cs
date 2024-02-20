using System.Text.Json.Serialization;

namespace TournamentTool.Commands;

public class PaceMan
{
    //public

    [JsonPropertyName("user")]
    public object? User { get; set; }

    [JsonPropertyName("nickname")]
    public object? Nickname { get; set; }

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
