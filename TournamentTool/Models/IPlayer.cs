namespace TournamentTool.Models;

public interface IPlayer
{
    public bool IsUsedInPov { get; set; }

    public string GetDisplayName();
    public string GetPersonalBest();
    public string GetHeadViewParametr();
    public string GetTwitchName();
    public bool IsFromWhiteList();

}
