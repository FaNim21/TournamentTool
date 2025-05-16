using System.Text.Json.Serialization;

namespace TournamentTool.Models;

public class PaceMan
{
    [JsonPropertyName("worldId")] 
    public string WorldID { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PacemanPaceMilestone> Splits { get; set; } = [];

    [JsonPropertyName("contextEventList")]
    public List<PacemanPaceMilestone> Advacements { get; set; } = [];

    [JsonPropertyName("itemData")]
    public PaceItemData ItemsData { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public long LastUpdate { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("isCheated")]
    public bool IsCheated { get; set; }

    public bool ShowOnlyLive { get; set; }


    public bool IsLive()
    {
        return !string.IsNullOrEmpty(User.TwitchName);
    }
}

public class PacemanPaceMilestone
{
    [JsonPropertyName("eventId")]
    public string SplitName { get; set; } = string.Empty;

    [JsonPropertyName("rta")]
    public long RTA { get; set; }

    [JsonPropertyName("igt")]
    public long IGT { get; set; }
}

public class PaceManUser
{
    [JsonPropertyName("uuid")] 
    public string? UUID { get; set; } = string.Empty;

    [JsonPropertyName("liveAccount")]
    public string? TwitchName { get; set; }
}

public struct PaceManStreamData
{
    [JsonPropertyName("twitchId")]
    public string MainID { get; set; }

    [JsonPropertyName("alt")]
    public string AltID { get; set; }
}

public class PaceItemData
{
    [JsonPropertyName("estimatedCounts")] 
    public Dictionary<string, int> EstimatedCounts { get; set; } = [];

    [JsonPropertyName("usages")] 
    public Dictionary<string, int> Usages { get; set; } = [];
}
