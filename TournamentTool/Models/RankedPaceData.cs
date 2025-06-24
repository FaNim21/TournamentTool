using System.Text.Json.Serialization;
using TournamentTool.Enums;

namespace TournamentTool.Models;

public class RankedPlayer
{
    [JsonPropertyName("uuid")] 
    public string UUID { get; set; } = string.Empty;

    [JsonPropertyName("nickname")] 
    public string NickName { get; set; } = string.Empty;

    [JsonPropertyName("roleType")]
    public byte RoleType { get; set; }

    [JsonPropertyName("eloRate")]
    public int? EloRate { get; set; }

    [JsonPropertyName("eloRank")]
    public int? EloRank { get; set; }
}
public struct RankedComplete
{
    [JsonPropertyName("player")]
    public string UUID { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }
}
public class RankedInventory
{
    [JsonPropertyName("splash_potion")]
    public int? SplashPotions { get; set; }

    [JsonPropertyName("gold_block")]
    public int GoldBlocks { get; set; }

    [JsonPropertyName("iron_ingot")]
    public int IronIngots { get; set; }

    [JsonPropertyName("obsidian")]
    public int Obsidian { get; set; }

    [JsonPropertyName("glowstone_dust")]
    public int GlowstoneDust { get; set; }

    [JsonPropertyName("string")]
    public int String { get; set; }

    [JsonPropertyName("crying_obsidian")]
    public int CryingObsidian { get; set; }

    [JsonPropertyName("ender_pearl")]
    public int EnderPearl { get; set; }

    [JsonPropertyName("iron_nugget")]
    public int IronNugger { get; set; }

    [JsonPropertyName("diamond")]
    public int Diamond { get; set; }

    [JsonPropertyName("white_bed")]
    public int WhiteBed { get; set; }

    [JsonPropertyName("glowstone")]
    public int GlowStone { get; set; }

    [JsonPropertyName("ender_eye")]
    public int EnderEye { get; set; }

    [JsonPropertyName("blaze_rod")]
    public int BlazeRod { get; set; }

    [JsonPropertyName("gold_ingot")]
    public int GoldIngot { get; set; }

    [JsonPropertyName("white_wool")]
    public int WhiteWool { get; set; }

    [JsonPropertyName("blaze_powder")]
    public int BlazePowder { get; set; }

    [JsonPropertyName("potion")]
    public int Potion { get; set; }
}
public class RankedTimeline
{
    [JsonPropertyName("uuid")] 
    public string UUID { get; set; } = string.Empty;

    [JsonPropertyName("type")] 
    public string Type { get; set; } = string.Empty; //maybe enum?? with all types ready

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("data")] 
    public int[] Data { get; set; } = [];

    [JsonPropertyName("shown")]
    public bool IsShown { get; set; }
}
public record RankedData
{
    [JsonPropertyName("matchType")]
    public string MatchType { get; init; } = string.Empty;

    [JsonPropertyName("category")] 
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("startTime")]
    public long StartTime { get; init; }

    [JsonPropertyName("players")]
    public RankedPlayer[] Players { get; init; } = [];

    [JsonPropertyName("completes")] 
    public RankedComplete[] Completes { get; init; } = [];

    [JsonPropertyName("inventories")]
    public Dictionary<string, RankedInventory> Inventories { get; init; } = [];

    [JsonPropertyName("timelines")]
    public List<RankedTimeline> Timelines { get; init; } = [];
}

public class RankedPaceData
{
    public RankedPlayer Player { get; init; } = new();
    public List<RankedTimeline> Timelines { get; init; } = [];
    public RankedInventory Inventory { get; set; } = new();
    public RankedComplete Completion { get; set; } = new();
    public int Resets { get; set; }
}
public class RankedBestSplit
{
    public string? PlayerName { get; set; }
    public RankedSplitType Type { get; set; }
    public long Time { get; set; }
}