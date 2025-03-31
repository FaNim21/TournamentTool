﻿using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.Utils;

namespace TournamentTool.ViewModels.Entities;

public class TournamentViewModel : BaseViewModel, ITournamentManager
{
    private Tournament _tournament;
    
    public ManagementData? ManagementData
    {
        get => _tournament.ManagementData;
        set
        {
            _tournament.ManagementData = value;
            PresetIsModified();
            OnPropertyChanged(nameof(ManagementData));
        }
    }

    public LeaderboardViewModel Leaderboard { get; set; }

    public ObservableCollection<Player> Players
    {
        get => _tournament.Players;
        set
        {
            _tournament.Players = value;
            OnPropertyChanged(nameof(Players));
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
        }
    }
    
    public int Port
    {
        get => _tournament.Port;
        set
        {
            _tournament.Port = value;
            OnPropertyChanged(nameof(Port));
            PresetIsModified();
        }
    }
    public string Password
    {
        get => _tournament.Password;
        set
        {
            _tournament.Password = value;
            OnPropertyChanged(nameof(Password));
            PresetIsModified();
        }
    }
    public string SceneCollection
    {
        get => _tournament.SceneCollection;
        set
        {
            _tournament.SceneCollection = value;
            OnPropertyChanged(nameof(SceneCollection));
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
        }
    }
    public bool SetPovPBText
    {
        get => _tournament.SetPovPBText;
        set
        {
            _tournament.SetPovPBText = value;
            OnPropertyChanged(nameof(SetPovPBText));
            PresetIsModified();
        }
    }
    public DisplayedNameType DisplayedNameType
    {
        get => _tournament.DisplayedNameType;
        set
        {
            _tournament.DisplayedNameType = value;
            OnPropertyChanged(nameof(DisplayedNameType));
            PresetIsModified();
        }
    }
    
    public int ApiRefreshRateMiliseconds
    {
        get => _tournament.ApiRefreshRateMiliseconds;
        set
        {
            _tournament.ApiRefreshRateMiliseconds = value < 1000 ? 1000 : value;
            OnPropertyChanged(nameof(ApiRefreshRateMiliseconds));
            PresetIsModified();
        }
    }
    public int PaceManRefreshRateMiliseconds
    {
        get => _tournament.PaceManRefreshRateMiliseconds;
        set
        {
            _tournament.PaceManRefreshRateMiliseconds = value < 3000 ? 3000 : value;
            OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
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
            PresetIsModified();
        }
    }
    public string? CreditsToText { set; get; }

    public Action? OnControllerModeChanged;
    public ControllerMode ControllerMode
    {
        get => _tournament.ControllerMode;
        set
        {
            if (_tournament.ControllerMode == value) return;
            
            _tournament.ControllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
            PresetIsModified();
            
            OnControllerModeChanged?.Invoke();
    
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
            PresetIsModified();
            OnPropertyChanged(nameof(RankedRoomDataPath));
        }
    }
    public string RankedRoomDataName
    {
        get => _tournament.RankedRoomDataName;
        set
        {
            _tournament.RankedRoomDataName = value;
            PresetIsModified();
            OnPropertyChanged(nameof(RankedRoomDataName));
        }
    }
    public int RankedRoomUpdateFrequency
    {
        get => _tournament.RankedRoomUpdateFrequency;
        set
        {
            _tournament.RankedRoomUpdateFrequency = value < 1000 ? 1000 : value;
            PresetIsModified();
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
    
    private bool _isPresetModified;
    public bool IsPresetModified
    {
        get => _isPresetModified;
        set
        {
            _isPresetModified = value;
            OnPropertyChanged(nameof(IsPresetModified));
        }
    }
    
    public bool HasBeenRemoved { get; set; } = true;


    public TournamentViewModel()
    {
        _tournament = new Tournament();
        Leaderboard = new LeaderboardViewModel(_tournament, this);
    }

    public void ChangeData(Tournament tournament)
    {
        if (tournament == null) return;

        Leaderboard.Clear();
        
        _tournament = tournament;
        Leaderboard = new LeaderboardViewModel(_tournament, this);
        
        SetupPreset();
        
        IsCurrentlyOpened = true;
        HasBeenRemoved = false;
        
        OnControllerModeChanged?.Invoke();
        
        PresetIsSaved();
    }

    private void SetupPreset()
    {
        Leaderboard.Setup();
        UpdatePlayers();
        UpdateGoodPacesTexts();
        
        Leaderboard.RefreshUI();
        RefreshUI();
        
        SetAlwaysOnTop();
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
            player.Initialize();
        }
    }
    public void UpdateGoodPacesTexts()
    {
        var time = TimeSpan.FromMilliseconds(Structure2GoodPaceMiliseconds).ToString(@"mm\:ss");
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
            PresetIsModified();
        });
    }
    public void RemovePlayer(Player player)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Remove(player);
            PresetIsModified();
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
    public Player? GetPlayerByUUID(string uuid)
    {
        foreach (var player in Players)
        {
            if (player.UUID != uuid) continue;
            return player;
        }
        
        return null;
    }
    public Player? GetPlayerByIGN(string ign)
    {
        foreach (var player in Players)
        {
            if (!player.InGameName!.Equals(ign)) continue;
            return player;
        }
        
        return null;
    }
    public string GetUUIDByIGN(string ign)
    {
        foreach (var player in Players)
        {
            if (!player.InGameName!.Equals(ign)) continue;
            return player.UUID;
        }
        
        return string.Empty;
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

    public void PresetIsModified()
    {
        //TODO: 9 kiedys zrobic bardziej zaawansowane przechwytywanie zmian z weryfikacja powrotu do danych przed zmiana itd
        //to sie tez tyczy problemow jak zmiana atrybutow w player, ponieawz nie jest bezposrednio powiazany z modelem wiec na razie
        // nie chce wprowadzac zmian z zapisywaniem itp itd tutaj
        IsPresetModified = true;
    }
    public void PresetIsSaved()
    {
        IsPresetModified = false;
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