using MultiOpener.Entities.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models.Ranking;
using TournamentTool.Modules.SidePanels;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Models;

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

    string GetPath() => Path.Combine(Consts.PresetsPath, Name + ".json");
}

public class TournamentPreset : BaseViewModel, IRenameItem, IPreset
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    } 

    private IPresetSaver? _presetSaver;
    private TournamentViewModel? _tournament;


    [JsonConstructor]
    public TournamentPreset(string name)
    {
        Name = name;
    }

    public void Setup(TournamentViewModel tournament, IPresetSaver presetSaver)
    {
        _tournament = tournament;
        _presetSaver = presetSaver;
    }

    public void ChangeName(string name)
    {
        if (string.IsNullOrEmpty(name) || Name.Equals(name)) return;

        var jsonName = name + ".json";
        var path = Path.Combine(Consts.PresetsPath, Name + ".json");

        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, jsonName);

        File.Move(path, newPath);
        Name = name;
        _tournament!.Name = name;
        _presetSaver!.SavePreset();
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }
}

[JsonDerivedType(typeof(SoloManagementData), typeDiscriminator: "Solo")]
[JsonDerivedType(typeof(PacemanManagementData), typeDiscriminator: "Paceman")]
[JsonDerivedType(typeof(RankedManagementData), typeDiscriminator: "Ranked")]
public abstract class ManagementData;

public class RankedManagementData : ManagementData
{
    public string CustomText { get; set; } = string.Empty;
    public int Rounds { get; set; } = 0;
    public long StartTime { get; set; } = 0;
    [JsonIgnore] public List<PrivRoomBestSplit> BestSplitsDatas { get; set; } = [];
    [JsonIgnore] public int Players { get; set; }
    [JsonIgnore] public int Completions { get; set; }
}

public class PacemanManagementData : ManagementData
{
    //TODO: 5 dane od api i rzeczy z zarzadzania pacemanem w kontrolerze
}

public class SoloManagementData : ManagementData
{
    public string LastFileName { get; set; } = string.Empty;
    //TODO: 5 Tez jakas zmienna pod ostatni timeline i fajny bylby record tutaj
}

public class Tournament : IPreset
{
    public ManagementData? ManagementData { get; set; }

    public Leaderboard Leaderboard { get; init; } = new();

    public List<Player> Players { get; init; } = [];

    public string Name { get; set; } = string.Empty;

    public bool IsAlwaysOnTop { get; set; } = true;
    public bool IsUsingTeamNames { get; set; }
    public bool IsUsingWhitelistOnPaceMan { get; set; } = true;
    public bool AddUnknownPacemanPlayersToWhitelist { get; set; } = false;
    public bool ShowOnlyLive { get; set; } = true;

    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string SceneCollection { get; set; } = string.Empty;
    public string FilterNameAtStartForSceneItems { get; set; } = "pov"; //

    public bool SetPovHeadsInBrowser { get; set; }
    public bool SetPovPBText { get; set; }
    public DisplayedNameType DisplayedNameType { get; set; }

    public bool IsUsingTwitchAPI { get; set; }
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
}
