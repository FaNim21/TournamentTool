namespace TournamentTool.Models;

public interface IPlayer
{
    public bool IsLive { get; set; }

    public bool IsUsedInPov { get; set; }
    public bool IsUsedInPreview { get; set; }

    public string DisplayName { get; }
    public string GetPersonalBest { get; }
    public string HeadViewParameter { get; }
    public string TwitchName { get; }
    public bool IsFromWhitelist { get; }
}
