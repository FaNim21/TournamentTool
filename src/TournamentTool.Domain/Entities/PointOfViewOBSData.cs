using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities;

public class PointOfViewOBSData
{
    public int ID { get; init; }
    public string GroupName { get; init; }
    public string SceneName { get; init; }
    public string SceneItemName { get; init; }
    
    public string TextFieldItemName { get; set; } = string.Empty;
    public string HeadItemName { get; set; } = string.Empty;
    public string PersonalBestItemName { get; set; } = string.Empty;
    
    public bool IsFromWhiteList { get; set; }
    public string DisplayedPlayer { get; set; } = string.Empty;
    public string PersonalBest { get; set; } = string.Empty;
    public string HeadViewParametr { get; set; } = string.Empty;
    public StreamDisplayInfo StreamDisplayInfo { get; set; } = new(string.Empty, StreamType.twitch);

    public int Volume { get; set; }
    public bool IsMuted { get; set; } = true;
    
    
    public PointOfViewOBSData(int id, string groupName, string sceneName, string sceneItemName)
    {
        ID = id;
        GroupName = groupName;
        SceneName = sceneName;
        SceneItemName = sceneItemName;
    }
}
