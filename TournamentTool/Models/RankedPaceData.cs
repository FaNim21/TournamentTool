using System.Text.Json.Serialization;
using TournamentTool.Converters.JSON;
using TournamentTool.Enums;

namespace TournamentTool.Models;

[JsonConverter(typeof(PrivRoomMatchStatusConverter))]
public enum MatchStatus
{
    idle,
    counting,
    generate,
    ready,
    running,
    done
}

public class PrivRoomAPIResult
{
    [JsonPropertyName("status")] public string Status { get; init; } = string.Empty;
    [JsonPropertyName("data")] public PrivRoomData Data { get; init; } = new();
}

public class PrivRoomData
{
    [JsonPropertyName("lastId")] public int? LastID { get; init; }
    [JsonPropertyName("type")] public int Type { get; init; }
    [JsonPropertyName("status")] public MatchStatus Status { get; init; }
    [JsonPropertyName("time")] public int Time { get; init; }
    
    [JsonPropertyName("players")] public PrivRoomPlayer[] Players { get; init; } = [];
    [JsonPropertyName("spectators")] public PrivRoomPlayer[] Spectators { get; init; } = [];
    [JsonPropertyName("completions")] public PrivRoomCompletion[] Completions { get; init; } = [];
    
    [JsonPropertyName("timelines")] public List<PrivRoomTimeline> Timelines { get; init; } = [];
}

public class PrivRoomPlayer
{
    [JsonPropertyName("uuid")] public string UUID { get; init; } = string.Empty;
    [JsonPropertyName("nickname")] public string InGameName { get; init; } = string.Empty;
    [JsonPropertyName("roleType")] public byte RoleType { get; init; }
    [JsonPropertyName("eloRate")] public int? EloRate { get; init; }
    [JsonPropertyName("eloRank")] public int? EloRank { get; init; }
    [JsonPropertyName("country")] public string Country { get; init; } = string.Empty;
}

public class PrivRoomTimeline
{
    [JsonPropertyName("uuid")] public string UUID { get; init; } = string.Empty;
    [JsonPropertyName("time")] public long Time { get; init; }
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
}

public class PrivRoomCompletion
{
    [JsonPropertyName("uuid")] public string UUID { get; init; } = string.Empty;
    [JsonPropertyName("time")] public long Time { get; init; }
}

public class PrivRoomInventory
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

public class PrivRoomPaceData
{
    public PrivRoomPlayer Player { get; init; } = new();
    public List<PrivRoomTimeline> Timelines { get; init; } = [];
    public PrivRoomInventory Inventory { get; set; } = new();
    public PrivRoomCompletion Completion { get; set; } = new();
    public int Resets { get; set; }
}

public class PrivRoomBestSplit
{
    public string? PlayerName { get; set; }
    public RankedSplitType Type { get; set; }
    public long Time { get; set; }
}