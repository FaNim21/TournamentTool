using System.Text.Json.Serialization;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;

namespace TournamentTool.Domain.Entities;

public enum DisplayedNameType
{
    None,
    Twitch,
    IGN,
    WhiteList
}

public interface IPreset
{
    string Name { get; set; }

    string GetPath(string presetsPath) => Path.Combine(presetsPath, Name + ".json");
}

[JsonDerivedType(typeof(PacemanManagementData), typeDiscriminator: "Paceman")]
[JsonDerivedType(typeof(RankedManagementData), typeDiscriminator: "Ranked")]
public abstract class ManagementData;

public class RankedManagementData : ManagementData
{
    public string CustomText { get; set; } = string.Empty;
    public int Rounds { get; set; }
    public long StartTime { get; set; }
    [JsonIgnore] public List<PrivRoomBestSplit> BestSplitsDatas { get; set; } = [];
    [JsonIgnore] public int Players { get; set; }
    [JsonIgnore] public int Completions { get; set; }
    [JsonIgnore] public bool RefreshUI { get; set; }
}

public class PacemanManagementData : ManagementData
{
    //TODO: 5 dane od api i rzeczy z zarzadzania pacemanem w kontrolerze
}

public class Tournament : IPreset
{
    //TODO: 0 To potrzebuje specjalnej troski przez to ze musze jako robic publish w binding engine dla tych danych w managementData
    public ManagementData? ManagementData { get; set; }
    public Leaderboard Leaderboard { get; init; } = new();

    public List<Player> Players { get; init; } = [];

    public string Name { get; set; } = string.Empty;

    public bool IsUsingTeamNames { get; set; }
    public bool IsUsingWhitelistOnPaceMan { get; set; } = true;
    public bool AddUnknownPacemanPlayersToWhitelist { get; set; } = false;
    public bool ShowOnlyLive { get; set; } = true;

    public string SceneCollection { get; set; } = string.Empty;
    public bool ShowStreamCategory { get; set; } = true;
    
    public int PaceManRefreshRateMiliseconds { get; set; } = 3000;

    public int Structure2GoodPaceMiliseconds { get; set; } = 270000; 
    public int FirstPortalGoodPaceMiliseconds { get; set; } = 360000;
    public int EnterStrongholdGoodPaceMiliseconds { get; set; } = 450000;
    public int EnterEndGoodPaceMiliseconds { get; set; } = 480000;
    public int CreditsGoodPaceMiliseconds { get; set; } = 600000;

    public ControllerMode ControllerMode { get; set; }

    public string RankedApiPlayerName { get; set; } = string.Empty;
    public string RankedApiKey { get; set; } = string.Empty;
    public bool AddUnknownRankedPlayersToWhitelist { get; set; }
    
    
    public void ClearPresetData()
    {
        IsUsingTeamNames = false;
        IsUsingWhitelistOnPaceMan = true;
        ShowOnlyLive = true;
        AddUnknownPacemanPlayersToWhitelist = false;

        SceneCollection = string.Empty;
        ShowStreamCategory = true;

        PaceManRefreshRateMiliseconds = 3000;

        Structure2GoodPaceMiliseconds = 270000;
        FirstPortalGoodPaceMiliseconds = 360000;
        EnterStrongholdGoodPaceMiliseconds = 450000;
        EnterEndGoodPaceMiliseconds = 480000;
        CreditsGoodPaceMiliseconds = 600000;

        RankedApiPlayerName = string.Empty;
        RankedApiKey = string.Empty;
        AddUnknownRankedPlayersToWhitelist = false;
    }
}