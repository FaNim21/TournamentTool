using MultiOpener.Entities.Interfaces;
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

public class Tournament : BaseViewModel, IRenameItem
{
    public string Name { get; set; } = string.Empty;

    public ObservableCollection<Player> Players { get; set; } = [];

    [JsonIgnore]
    public PresetManagerViewModel? MainViewModel;

    public int Port { get; set; } = 4455;
    public string Password { get; set; } = string.Empty;
    public string SceneCollection { get; set; } = string.Empty;

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

    public bool ShowLiveOnlyForMinecraftCategory { get; set; } = true;

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


    [JsonConstructor]
    public Tournament(string name = "")
    {
        Name = name;
    }
    public Tournament(PresetManagerViewModel mainViewModel, string name = "")
    {
        MainViewModel = mainViewModel;
        Name = name;
    }

    public void UpdatePlayers()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].LoadHead();
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

    public void ChangeName(string name)
    {
        var jsonName = name + ".json";
        var path = GetPath();

        var directoryName = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(directoryName, jsonName);

        File.Move(path, newPath);
        Name = name;
        OnPropertyChanged(nameof(Name));
        MainViewModel!.SavePresetCommand.Execute(this);
    }

    public string GetPath()
    {
        return Path.Combine(Consts.PresetsPath, Name + ".json");
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
}
