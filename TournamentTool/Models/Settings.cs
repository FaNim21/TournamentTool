namespace TournamentTool.Models;

public class Settings
{
    public string LastOpenedPresetName { get; set; } = string.Empty;
    
    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string FilterNameAtStartForSceneItems { get; set; } = "pov";

    public bool SaveTwitchRefreshToken { get; set; } = false;
}