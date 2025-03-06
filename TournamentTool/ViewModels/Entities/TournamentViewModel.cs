using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using TournamentTool.Models;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class TournamentViewModel : BaseViewModel, ITournamentManager
{
    private Tournament _tournament = new();
    
    public ManagementData? ManagementData
    {
        get => _tournament.ManagementData;
        set
        {
            _tournament.ManagementData = value;
            OnPropertyChanged(nameof(ManagementData));
        }
    }
    
    public ObservableCollection<Player> Players
    {
        get => _tournament.Players;
        set
        {
            _tournament.Players = value;
            OnPropertyChanged(nameof(Players));
        }
    }
    
    public string Name
    {
        get => _tournament.Name;
        set
        {
            _tournament.Name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    
    public bool IsAlwaysOnTop
    {
        get => _tournament.IsAlwaysOnTop;
        set
        {
            _tournament.IsAlwaysOnTop = value;
            OnPropertyChanged(nameof(IsAlwaysOnTop));
            SetAlwaysOnTop();
        }
    }
    public bool IsUsingTeamNames
    {
        get => _tournament.IsUsingTeamNames;
        set
        {
            if (_tournament.IsUsingTeamNames == value) return;
                
            _tournament.IsUsingTeamNames = value;
            OnPropertyChanged(nameof(IsUsingTeamNames));
            UpdateTeamNamesForPlayers();
        }
    }
    public bool IsUsingWhitelistOnPaceMan
    {
        get => _tournament.IsUsingWhitelistOnPaceMan;
        set
        {
            _tournament.IsUsingWhitelistOnPaceMan = value;
            OnPropertyChanged(nameof(IsUsingWhitelistOnPaceMan));
        }
    }
    
    public int Port
    {
        get => _tournament.Port;
        set
        {
            _tournament.Port = value;
            OnPropertyChanged(nameof(Port));
        }
    }
    public string Password
    {
        get => _tournament.Password;
        set
        {
            _tournament.Password = value;
            OnPropertyChanged(nameof(Password));
        }
    }
    public string SceneCollection
    {
        get => _tournament.SceneCollection;
        set
        {
            _tournament.SceneCollection = value;
            OnPropertyChanged(nameof(SceneCollection));
        }
    }
    public string FilterNameAtStartForSceneItems
    {
        get => _tournament.FilterNameAtStartForSceneItems;
        set
        {
            if (!value.StartsWith("head", StringComparison.OrdinalIgnoreCase))
                _tournament.FilterNameAtStartForSceneItems = value;
    
            OnPropertyChanged(nameof(FilterNameAtStartForSceneItems));
        }
    }
    
    public bool IsUsingTwitchAPI
    {
        get => _tournament.IsUsingTwitchAPI;
        set
        {
            if (_tournament.IsUsingTwitchAPI == value) return;
                
            _tournament.IsUsingTwitchAPI = value;
            OnPropertyChanged(nameof(IsUsingTwitchAPI));
            UpdateCategoryForPlayers();
        }
    }
    public bool ShowStreamCategory
    {
        get => _tournament.ShowStreamCategory;
        set
        {
            if (_tournament.ShowStreamCategory == value) return;
                
            _tournament.ShowStreamCategory = value;
            OnPropertyChanged(nameof(ShowStreamCategory));
            UpdateCategoryForPlayers();
        }
    }
    
    public bool SetPovHeadsInBrowser
    {
        get => _tournament.SetPovHeadsInBrowser;
        set
        {
            _tournament.SetPovHeadsInBrowser = value;
            OnPropertyChanged(nameof(SetPovHeadsInBrowser));
        }
    }
    public bool SetPovPBText
    {
        get => _tournament.SetPovPBText;
        set
        {
            _tournament.SetPovPBText = value;
            OnPropertyChanged(nameof(SetPovPBText));
        }
    }
    public DisplayedNameType DisplayedNameType
    {
        get => _tournament.DisplayedNameType;
        set
        {
            _tournament.DisplayedNameType = value;
            OnPropertyChanged(nameof(DisplayedNameType));
        }
    }
    
    public int ApiRefreshRateMiliseconds
    {
        get => _tournament.ApiRefreshRateMiliseconds;
        set
        {
            _tournament.ApiRefreshRateMiliseconds = value < 1000 ? 1000 : value;
            OnPropertyChanged(nameof(ApiRefreshRateMiliseconds));
        }
    }
    public int PaceManRefreshRateMiliseconds
    {
        get => _tournament.PaceManRefreshRateMiliseconds;
        set
        {
            _tournament.PaceManRefreshRateMiliseconds = value < 3000 ? 3000 : value;
            OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
        }
    }
    
    public int Structure2GoodPaceMiliseconds
    {
        get => _tournament.Structure2GoodPaceMiliseconds;
        set
        {
            _tournament.Structure2GoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            Structure2ToText = $"Structure 2 (sub {time})";
            OnPropertyChanged(nameof(Structure2ToText));
            OnPropertyChanged(nameof(Structure2GoodPaceMiliseconds));
        }
    }
    public string? Structure2ToText { set; get; }
    
    public int FirstPortalGoodPaceMiliseconds
    {
        get => _tournament.FirstPortalGoodPaceMiliseconds;
        set
        {
            _tournament.FirstPortalGoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            FirstPortalToText = $"First Portal (sub {time})";
            OnPropertyChanged(nameof(FirstPortalToText));
            OnPropertyChanged(nameof(FirstPortalGoodPaceMiliseconds));
        }
    }
    public string? FirstPortalToText { set; get; }
    
    public int EnterStrongholdGoodPaceMiliseconds
    {
        get => _tournament.EnterStrongholdGoodPaceMiliseconds;
        set
        {
            _tournament.EnterStrongholdGoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            EnterStrongholdToText = $"Enter Stronghold (sub {time})";
            OnPropertyChanged(nameof(EnterStrongholdToText));
            OnPropertyChanged(nameof(EnterStrongholdGoodPaceMiliseconds));
        }
    }
    public string? EnterStrongholdToText { set; get; }
    
    public int EnterEndGoodPaceMiliseconds
    {
        get => _tournament.EnterEndGoodPaceMiliseconds;
        set
        {
            _tournament.EnterEndGoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            EnterEndToText = $"Enter End (sub {time})";
            OnPropertyChanged(nameof(EnterEndToText));
            OnPropertyChanged(nameof(EnterEndGoodPaceMiliseconds));
        }
    }
    public string? EnterEndToText { set; get; }
    
    public int CreditsGoodPaceMiliseconds
    {
        get => _tournament.CreditsGoodPaceMiliseconds;
        set
        {
            _tournament.CreditsGoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            CreditsToText = $"Finish (sub {time})";
            OnPropertyChanged(nameof(CreditsToText));
            OnPropertyChanged(nameof(CreditsGoodPaceMiliseconds));
        }
    }
    public string? CreditsToText { set; get; }
    
    public ControllerMode ControllerMode
    {
        get => _tournament.ControllerMode;
        set
        {
            _tournament.ControllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
    
            if (_tournament.ControllerMode == value) return;
    
            if (value == ControllerMode.Ranked)
                ManagementData = new RankedManagementData();
            else if (value == ControllerMode.PaceMan)
                ManagementData = new PacemanManagementData();
        }
    }
    
    public string RankedRoomDataPath
    {
        get => _tournament.RankedRoomDataPath;
        set
        {
            _tournament.RankedRoomDataPath = value;
            OnPropertyChanged(nameof(RankedRoomDataPath));
        }
    }
    public string RankedRoomDataName
    {
        get => _tournament.RankedRoomDataName;
        set
        {
            _tournament.RankedRoomDataName = value;
            OnPropertyChanged(nameof(RankedRoomDataName));
        }
    }
    public int RankedRoomUpdateFrequency
    {
        get => _tournament.RankedRoomUpdateFrequency;
        set
        {
            _tournament.RankedRoomUpdateFrequency = value < 1000 ? 1000 : value;

            OnPropertyChanged(nameof(RankedRoomUpdateFrequency));
        }
    }

    private bool _isCurrentlyOpened;
    public bool IsCurrentlyOpened
    {
        get => _isCurrentlyOpened;
        set
        {
            _isCurrentlyOpened = value;
            OnPropertyChanged(nameof(IsCurrentlyOpened));
        }
    }

    public bool HasBeenRemoved { get; set; } = true;


    public void ChangeData(Tournament tournament)
    {
        if (tournament == null) return;
        
        _tournament = tournament;
        
        UpdatePlayers();
        UpdateGoodPacesTexts();
        
        RefreshUI();
        IsCurrentlyOpened = true;
        HasBeenRemoved = false;
    }
    
    public void RefreshUI()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsAlwaysOnTop));
        OnPropertyChanged(nameof(IsUsingTeamNames));
        OnPropertyChanged(nameof(IsUsingWhitelistOnPaceMan));
        
        OnPropertyChanged(nameof(Port));
        OnPropertyChanged(nameof(Password));
        OnPropertyChanged(nameof(SceneCollection));
        OnPropertyChanged(nameof(FilterNameAtStartForSceneItems));
        
        OnPropertyChanged(nameof(SetPovHeadsInBrowser));
        OnPropertyChanged(nameof(SetPovPBText));
        OnPropertyChanged(nameof(DisplayedNameType));
        
        OnPropertyChanged(nameof(IsUsingTwitchAPI));
        OnPropertyChanged(nameof(ShowStreamCategory));
        
        OnPropertyChanged(nameof(ApiRefreshRateMiliseconds));
        OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
        
        OnPropertyChanged(nameof(Structure2ToText));
        OnPropertyChanged(nameof(Structure2GoodPaceMiliseconds));
        OnPropertyChanged(nameof(FirstPortalToText));
        OnPropertyChanged(nameof(FirstPortalGoodPaceMiliseconds));
        OnPropertyChanged(nameof(EnterStrongholdToText));
        OnPropertyChanged(nameof(EnterStrongholdGoodPaceMiliseconds));
        OnPropertyChanged(nameof(EnterEndToText));
        OnPropertyChanged(nameof(EnterEndGoodPaceMiliseconds));
        OnPropertyChanged(nameof(CreditsToText));
        OnPropertyChanged(nameof(CreditsGoodPaceMiliseconds));
        
        OnPropertyChanged(nameof(ControllerMode));
        
        OnPropertyChanged(nameof(RankedRoomDataPath));
        OnPropertyChanged(nameof(RankedRoomDataName));
        OnPropertyChanged(nameof(RankedRoomUpdateFrequency));
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
    
    public void AddPlayer(Player player)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Add(player);
        });
    }
    public void RemovePlayer(Player player)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Remove(player);
        });
    }
    
    public bool ContainsDuplicates(Player findPlayer, Guid? excludeID = null)
    {
        foreach (var player in Players)
        {
            if (excludeID.HasValue && player.Id == excludeID.Value) continue;
            if (player.Equals(findPlayer)) return true;
        }
     
        return false;
    }
    public bool ContainsDuplicatesNoDialog(Player findPlayer, Guid? excludeID = null)
    {
        foreach (var player in Players)
        {
            if (excludeID.HasValue && player.Id == excludeID.Value) continue;
            if (player.EqualsNoDialog(findPlayer)) return true;
        }
    
        return false;
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
    
    public void ClearPlayerStreamData()
    {
        for (int i = 0; i < Players.Count; i++)
            Players[i].StreamData.LiveData.Clear(false);
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
        IsAlwaysOnTop = true;
        IsUsingTeamNames = false;
        IsUsingWhitelistOnPaceMan = true;
        
        Port = 4455;
        Password = string.Empty;
        SceneCollection = string.Empty;
        FilterNameAtStartForSceneItems = "pov";
    
        SetPovHeadsInBrowser = false;
        SetPovPBText = false;
        DisplayedNameType = DisplayedNameType.None;
    
        IsUsingTwitchAPI = false;
        ShowStreamCategory = true;

        ApiRefreshRateMiliseconds = 1000;
        PaceManRefreshRateMiliseconds = 3000;
    
        Structure2GoodPaceMiliseconds = 270000;
        FirstPortalGoodPaceMiliseconds = 360000;
        EnterStrongholdGoodPaceMiliseconds = 450000;
        EnterEndGoodPaceMiliseconds = 480000;
        CreditsGoodPaceMiliseconds = 600000;
        
        ControllerMode = ControllerMode.None;
        
        RankedRoomDataPath = string.Empty;
        RankedRoomDataName = "spectate_match.json";
        RankedRoomUpdateFrequency = 1000;
    }

    public void Delete()
    {
        HasBeenRemoved = true;
        IsCurrentlyOpened = false;
    }
    
    private void UpdateCategoryForPlayers()
    {
        foreach (var player in Players)
        {
            player.ShowCategory(ShowStreamCategory && IsUsingTwitchAPI);
        }
    }
    private void UpdateTeamNamesForPlayers()
    {
        foreach (var player in Players)
        {
            player.ShowTeamName(IsUsingTeamNames);
        }
    }

    private void SetAlwaysOnTop()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Topmost = IsAlwaysOnTop;
            }
        });
    }

    public bool IsNullOrEmpty()
    {
        return _tournament == null || HasBeenRemoved;
    }

    public Tournament GetData() => _tournament;
}