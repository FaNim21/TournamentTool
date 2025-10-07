using System.IO;
using System.Windows.Media;

namespace TournamentTool.Utils;

public static class Consts
{
    public const string Version = "v0.12.0";
    
    public static bool IsTesting { get; set; }

    public static readonly string AppdataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TournamentTool");
    public static readonly string PresetsPath = Path.Combine(AppdataPath, "Presets");
    public static readonly string AppAPIPath = Path.Combine(AppdataPath, "API");
    public static readonly string LogsPath = Path.Combine(AppdataPath, "Logs");
    public static readonly string ScriptsPath = Path.Combine(AppdataPath, "Scripts");
    public static readonly string LeaderboardScriptsPath = Path.Combine(ScriptsPath, "Leaderboard");

    public const string RedirectURL = "http://localhost:8080/redirect/";

    public const string PaceManAPI = "https://paceman.gg/api/ars/liveruns";
    public const string PaceManTwitchAPI = "https://paceman.gg/api/us/twitch";
    public const string PaceManUserAPI = "https://paceman.gg/api/us/user";
    public const string PaceManGetUserAPI = "https://paceman.gg/api/us/getuser?uuid=";

    public const float AspectRatio = 16.0f / 9.0f;
    
    public static readonly Color LiveColor = Color.FromRgb(0, 255, 127);
    public static readonly Color OfflineColor = Color.FromRgb(201, 61, 59);
    public static readonly Color DefaultColor = Color.FromRgb(220, 220, 220);

    //https://paceman.gg/api/us/user endpoint which takes in
    //https://paceman.gg/api/us/user?name=FaNim21&sortByTime=0

    //https://paceman.gg/stats/api/ tu jest duzo info odnosnie uzywania api

}
