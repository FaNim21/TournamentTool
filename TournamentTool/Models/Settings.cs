namespace TournamentTool.Models;

public enum HeadAPIType
{
    minotar,
    mcheads,
}

public class Settings
{
    public string LastOpenedPresetName { get; set; } = string.Empty;
    
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string FilterNameAtStartForSceneItems { get; set; } = "pov";

    public bool SaveTwitchToken { get; set; } = true;
    public bool IsAlwaysOnTop { get; set; } = true;

    public HeadAPIType HeadAPIType { get; set; }
}

public class APIKeys
{
    public string CustomTwitchClientID { get; set; } = string.Empty;
    public string TwitchAccessToken { get; set; } = string.Empty;

    public string MCSRRankedAPI { get; set; } = string.Empty;
    public string PacemanAPI { get; set; } = string.Empty;
}