namespace TournamentTool.Core.Utils;

public static class Consts
{
    public const string Version = "v1.0.0-PREVIEW19";
    
    public static bool IsTesting { get; set; }

    public static readonly string AppdataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TournamentTool");
    public static string PresetsPath => IsTesting ? Path.Combine(AppdataPath, "TestPresets") : Path.Combine(AppdataPath, "Presets");
    public static readonly string AppAPIPath = Path.Combine(AppdataPath, "API");
    public static readonly string LogsPath = Path.Combine(AppdataPath, "Logs");
    public static readonly string ScriptsPath = Path.Combine(AppdataPath, "Scripts");
    public static readonly string LeaderboardScriptsPath = Path.Combine(ScriptsPath, "Leaderboard");

    public const string RedirectURL = "http://localhost:8080/";

    public const string PacemanAPI = "https://paceman.gg/api/ars/liveruns";
    public const string PacemanEventListAPI = "https://paceman.gg/api/cs/eventlist";
    public const string PaceManTwitchAPI = "https://paceman.gg/api/us/twitch";
    public const string PaceManUserAPI = "https://paceman.gg/api/us/user";
    public const string PaceManGetUserAPI = "https://paceman.gg/api/us/getuser?uuid=";

    public const float AspectRatio = 16.0f / 9.0f;

    
    public const string FocusedPovColor = "#99e0ff";
    public const string UnFocusedPovColor = "#66b3cc";
    
    public const string LiveColor = "#00ff7f";
    public const string OfflineColor = "#c93d3b";
    public const string DefaultColor = "#dcdcdc";
    
    public const string GoldPaceColor = "#ffd700";
    public const string NormalPaceColor = "#f5deb3";
    
    public const string InfoColor = "#96cdee";
    public const string WarningColor = "#ffde21";
    public const string DebugColor = "#00a300";
    public const string ErrorColor = "#d43f3f";

    public const string GrayColor = "#808080";
    public const string LightBlueColor = "#ADD8E6";

    //https://paceman.gg/api/us/user endpoint which takes in
    //https://paceman.gg/api/us/user?name=FaNim21&sortByTime=0

    //https://paceman.gg/stats/api/ tu jest duzo info odnosnie uzywania api

}
