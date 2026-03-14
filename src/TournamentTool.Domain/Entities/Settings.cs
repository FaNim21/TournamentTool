using TournamentTool.Domain.Obs;

namespace TournamentTool.Domain.Entities;

public enum HeadAPIType
{
    minotar,
    mcheads,
}

public class Settings
{
    // General
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string FilterNameAtStartForSceneItems { get; set; } = "pov";

    public bool SaveTwitchToken { get; set; } = true;
    public bool AutoLoginToTwitch { get; set; } = true;
    public bool IsAlwaysOnTop { get; set; } = true;
    public bool SaveRankedPrivRoomDataOnSeedFinish { get; set; } = true;

    public HeadAPIType HeadAPIType { get; set; }
    
    // Console/logs
    public bool SaveLogsAfterShutdown { get; set; }
    public int ConsoleLogsLimit { get; set; } = 200;
}

public class APIKeys
{
    public string CustomTwitchClientID { get; set; } = string.Empty;
    public string TwitchAccessToken { get; set; } = string.Empty;

    public string MCSRRankedAPI { get; set; } = string.Empty;
    public string PacemanAPI { get; set; } = string.Empty;
}

public sealed class AppCache
{
    public string LastOpenedPresetName { get; set; } = string.Empty;
    
    public Dictionary<string, PresetOrderData> PresetsOrder { get; init; } = [];
    public bool IsConsoleWindowed { get; set; } = false;

    public Dictionary<string, SceneItemConfiguration> SceneItemConfigs { get; init; } = [];
}

public sealed class PresetOrderData
{
    public int index { get; set; } = int.MaxValue;
}

public record SceneItemConfiguration(InputKind InputKind, string BindingPath);