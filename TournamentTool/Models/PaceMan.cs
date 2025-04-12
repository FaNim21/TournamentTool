using System.Text.Json.Serialization;

namespace TournamentTool.Models;

public enum SplitType
{
    none,
    enter_nether,
    structure_1,
    structure_2,
    first_portal,
    second_portal,
    enter_stronghold,
    enter_end,
    credits
}

public class PaceMan
{
    [JsonPropertyName("user")]
    public PaceManUser User { get; set; } = new();

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("eventList")]
    public List<PaceSplitsList> Splits { get; set; } = [];

    [JsonPropertyName("contextEventList")]
    public List<PaceSplitsList> Advacements { get; set; } = [];

    [JsonPropertyName("itemData")]
    public PaceItemData ItemsData { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public long LastUpdate { get; set; }

    [JsonPropertyName("isHidden")]
    public bool IsHidden { get; set; }

    [JsonPropertyName("isCheated")]
    public bool IsCheated { get; set; }
    
    
    public bool IsLive()
    {
        return !string.IsNullOrEmpty(User.TwitchName);
    }
}

public class PaceSplitsList
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
    public string? UUID { get; set; }

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
