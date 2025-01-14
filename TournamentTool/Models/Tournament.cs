﻿using MultiOpener.Entities.Interfaces;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Text.Json.Serialization;
using System.Windows;
using TournamentTool.Utils;
using TournamentTool.ViewModels;

namespace TournamentTool.Models;

public enum DisplayedNameType
{
    None,
    Twitch,
    IGN,
    WhiteList
}

public enum ControllerMode
{
    None,
    PaceMan,
    Ranked,
}

public interface IPreset
{
    string Name { get; set; }

    string GetPath();
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

    private PresetManagerViewModel? PresetViewModel;


    [JsonConstructor]
    public TournamentPreset(string name)
    {
        Name = name;
    }

    public void Setup(PresetManagerViewModel presetManagerViewModel)
    {
        PresetViewModel = presetManagerViewModel;
    }

    public void ChangeName(string name)
    {
        if (PresetViewModel == null) return;
        if (string.IsNullOrEmpty(name) || Name.Equals(name)) return;

        var jsonName = name + ".json";
        var path = Path.Combine(Consts.PresetsPath, Name + ".json");

        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, jsonName);

        File.Move(path, newPath);
        Name = name;
        PresetViewModel.LoadedPreset!.Name = name;
        PresetViewModel.SaveLastOpened(name);
        PresetViewModel.SavePreset();
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }
}

[JsonDerivedType(typeof(ManagementTest), typeDiscriminator: "Test")]
public class ManagementBaseTest
{

}

public class ManagementTest : ManagementBaseTest
{
    public int JakiesGowno { get; set; } = 1234;
}

public class Tournament : BaseViewModel, IPreset
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

    public ManagementBaseTest ManagementData { get; set; }

    public ObservableCollection<Player> Players { get; set; } = [];

    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string SceneCollection { get; set; } = string.Empty;

    private bool _isAlwaysOnTop = true;
    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            _isAlwaysOnTop = value;
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Application.Current.MainWindow.Topmost = value;
            });
            OnPropertyChanged(nameof(IsAlwaysOnTop));
        }
    }

    private string _filterNameAtStartForSceneItems = "pov";
    public string FilterNameAtStartForSceneItems
    {
        get => _filterNameAtStartForSceneItems;
        set
        {
            if (!value.StartsWith("head", StringComparison.OrdinalIgnoreCase))
                _filterNameAtStartForSceneItems = value;

            OnPropertyChanged(nameof(FilterNameAtStartForSceneItems));
        }
    }

    private bool _isUsingTwitchAPI = true;
    public bool IsUsingTwitchAPI
    {
        get => _isUsingTwitchAPI;
        set
        {
            _isUsingTwitchAPI = value;
            OnPropertyChanged(nameof(IsUsingTwitchAPI));
        }
    }

    public bool IsUsingWhitelistOnPaceMan { get; set; } = true;

    public bool SetPovHeadsInBrowser { get; set; } = false;
    public bool SetPovPBText { get; set; } = false;

    private DisplayedNameType _displayedNameType;
    public DisplayedNameType DisplayedNameType
    {
        get => _displayedNameType;
        set
        {
            _displayedNameType = value;
            OnPropertyChanged(nameof(DisplayedNameType));
        }
    }

    public bool ShowLiveOnlyForMinecraftCategory { get; set; } = true;

    private int _apiRefreshRateMiliseconds = 1000;
    public int ApiRefreshRateMiliseconds
    {
        get => _apiRefreshRateMiliseconds;
        set
        {
            if (value < 1000)
                _apiRefreshRateMiliseconds = 1000;
            else
                _apiRefreshRateMiliseconds = value;
            OnPropertyChanged(nameof(ApiRefreshRateMiliseconds));
        }
    }

    private int _paceManRefreshRateMiliseconds = 3000;
    public int PaceManRefreshRateMiliseconds
    {
        get => _paceManRefreshRateMiliseconds;
        set
        {
            if (value < 3000)
                _paceManRefreshRateMiliseconds = 3000;
            else
                _paceManRefreshRateMiliseconds = value;
            OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
        }
    }

    private int _structure2GoodPaceMiliseconds = 270000;
    public int Structure2GoodPaceMiliseconds
    {
        get => _structure2GoodPaceMiliseconds;
        set
        {
            _structure2GoodPaceMiliseconds = value;

            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            Structure2ToText = $"Structure 2 (sub {time})";
            OnPropertyChanged(nameof(Structure2ToText));

            OnPropertyChanged(nameof(Structure2GoodPaceMiliseconds));
        }
    }
    public string? Structure2ToText { set; get; }

    private int _firstPortalGoodPaceMiliseconds = 360000;
    public int FirstPortalGoodPaceMiliseconds
    {
        get => _firstPortalGoodPaceMiliseconds;
        set
        {
            _firstPortalGoodPaceMiliseconds = value;

            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            FirstPortalToText = $"First Portal (sub {time})";
            OnPropertyChanged(nameof(FirstPortalToText));

            OnPropertyChanged(nameof(FirstPortalGoodPaceMiliseconds));
        }
    }
    public string? FirstPortalToText { set; get; }

    private int _enterStrongholdGoodPaceMiliseconds = 450000;
    public int EnterStrongholdGoodPaceMiliseconds
    {
        get => _enterStrongholdGoodPaceMiliseconds;
        set
        {
            _enterStrongholdGoodPaceMiliseconds = value;

            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            EnterStrongholdToText = $"Enter Stronghold (sub {time})";
            OnPropertyChanged(nameof(EnterStrongholdToText));

            OnPropertyChanged(nameof(EnterStrongholdGoodPaceMiliseconds));
        }
    }
    public string? EnterStrongholdToText { set; get; }

    private int _enterEndGoodPaceMiliseconds = 480000;
    public int EnterEndGoodPaceMiliseconds
    {
        get => _enterEndGoodPaceMiliseconds;
        set
        {
            _enterEndGoodPaceMiliseconds = value;

            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            EnterEndToText = $"Enter End (sub {time})";
            OnPropertyChanged(nameof(EnterEndToText));

            OnPropertyChanged(nameof(EnterEndGoodPaceMiliseconds));
        }
    }
    public string? EnterEndToText { set; get; }

    private int _creditsGoodPaceMiliseconds = 600000;
    public int CreditsGoodPaceMiliseconds
    {
        get => _creditsGoodPaceMiliseconds;
        set
        {
            _creditsGoodPaceMiliseconds = value;

            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            CreditsToText = $"Finish (sub {time})";
            OnPropertyChanged(nameof(CreditsToText));

            OnPropertyChanged(nameof(CreditsGoodPaceMiliseconds));
        }
    }
    public string? CreditsToText { set; get; }

    private ControllerMode _controllerMode;
    public ControllerMode ControllerMode
    {
        get => _controllerMode;
        set
        {
            _controllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
        }
    }

    private string _rankedRoomDataPath = string.Empty;
    public string RankedRoomDataPath
    {
        get => _rankedRoomDataPath;
        set
        {
            _rankedRoomDataPath = value;
            OnPropertyChanged(nameof(RankedRoomDataPath));
        }
    }

    private string _rankedRoomDataName = "spectate_match.json";
    public string RankedRoomDataName
    {
        get => _rankedRoomDataName;
        set
        {
            _rankedRoomDataName = value;
            OnPropertyChanged(nameof(RankedRoomDataName));
        }
    }

    private int _rankedRoomUpdateFrequency = 1000;
    public int RankedRoomUpdateFrequency
    {
        get => _rankedRoomUpdateFrequency;
        set
        {
            if (value < 1000)
                _rankedRoomUpdateFrequency = 1000;
            else
                _rankedRoomUpdateFrequency = value;

            OnPropertyChanged(nameof(RankedRoomUpdateFrequency));
        }
    }


    [JsonConstructor]
    public Tournament(string name = "")
    {
        Name = name;

        ManagementData = new ManagementTest();

    }

    public void ClearPlayerStreamData()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            var player = Players[i];
            player.StreamData.LiveData.Clear(false);
        }
    }

    public void UpdatePlayers()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            var player = Players[i];
            player.LoadHead();
        }
    }
    public void UpdateGoodPacesTexts()
    {
        string time;
        time = TimeSpan.FromMilliseconds(Structure2GoodPaceMiliseconds).ToString(@"mm\:ss");
        Structure2ToText = $"Structure 2 (sub {time})";

        time = TimeSpan.FromMilliseconds(FirstPortalGoodPaceMiliseconds).ToString(@"mm\:ss");
        FirstPortalToText = $"First Portal (sub {time})";

        time = TimeSpan.FromMilliseconds(EnterStrongholdGoodPaceMiliseconds).ToString(@"mm\:ss");
        EnterStrongholdToText = $"Enter Stronghold (sub {time})";

        time = TimeSpan.FromMilliseconds(EnterEndGoodPaceMiliseconds).ToString(@"mm\:ss");
        EnterEndToText = $"Enter End (sub {time})";

        time = TimeSpan.FromMilliseconds(CreditsGoodPaceMiliseconds).ToString(@"mm\:ss");
        CreditsToText = $"Finish (sub {time})";
    }

    public void Validate()
    {
        //TODO: 0 Trzeba zrobic baze do tego zeby przy zmianie danych presetu validowac dane
        UpdatePlayers();
        UpdateGoodPacesTexts();

        if (PaceManRefreshRateMiliseconds < 3000)
        {
            PaceManRefreshRateMiliseconds = 3000;
        }

        //TODO: 9 add some validations or change it to some cleaner version
    }

    public void AddPlayer(Player player)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Add(player);
        });
    }

    public bool IsNameDuplicate(string? twitchName)
    {
        if (string.IsNullOrEmpty(twitchName)) return false;
        int n = Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Players[i];
            if (current.StreamData.Main.Equals(twitchName, StringComparison.OrdinalIgnoreCase))
                return true;

            if (current.StreamData.Alt.Equals(twitchName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
        //return Players.Any(player => player.TwitchName!.Equals(twitchName, StringComparison.OrdinalIgnoreCase));
    }

    public Player? GetPlayerByTwitchName(string twitchName)
    {
        int n = Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Players[i];
            if (current.StreamData.ExistName(twitchName))
                return current;
        }
        return null;
    }

    public void ClearFromController()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].ClearFromController();
    }

    public void ClearPlayersFromPOVS()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].IsUsedInPov = false;
            Players[i].IsUsedInPreview = false;
        }
    }

    public void Clear()
    {
        Port = 4455;
        Password = string.Empty;
        SceneCollection = string.Empty;

        Players = [];
        FilterNameAtStartForSceneItems = "pov";
        IsUsingTwitchAPI = true;
        IsUsingWhitelistOnPaceMan = true;

        SetPovHeadsInBrowser = false;
        SetPovPBText = false;
        DisplayedNameType = DisplayedNameType.None;
        ControllerMode = ControllerMode.None;

        ShowLiveOnlyForMinecraftCategory = true;
        PaceManRefreshRateMiliseconds = 10000;
        IsAlwaysOnTop = true;

        Structure2GoodPaceMiliseconds = 270000;
        FirstPortalGoodPaceMiliseconds = 360000;
        EnterStrongholdGoodPaceMiliseconds = 450000;
        EnterEndGoodPaceMiliseconds = 480000;
        CreditsGoodPaceMiliseconds = 600000;
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
    }
}
