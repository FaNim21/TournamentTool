using System.IO;

namespace TournamentTool.Utils;

public static class Consts
{
    public const string Version = "v0.3.0";

    public static readonly string AppdataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TournamentTool");
    public static readonly string PresetsPath = Path.Combine(AppdataPath, "Presets");
}
