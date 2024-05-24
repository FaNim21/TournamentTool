namespace TournamentTool.Models;

public interface ITwitchPovInformation
{
    public string GetDisplayName();
    public string GetPersonalBest();
    public string GetHeadViewParametr();
    public string GetTwitchName();
    public bool IsFromWhiteList();
}
