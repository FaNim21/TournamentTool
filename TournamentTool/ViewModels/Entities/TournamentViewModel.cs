using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;

namespace TournamentTool.ViewModels.Entities;

public class TournamentViewModel : BaseViewModel, INotifyDataErrorInfo, ITournamentManager, IPresetInfo, INotifyPresetModification
{
    private readonly Dictionary<string, List<string>> _errors = [];
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _errors.Count != 0;
    
   
    private Tournament _tournament = new();
    
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

    public Leaderboard Leaderboard => _tournament.Leaderboard;

    private ObservableCollection<PlayerViewModel> _players = [];
    public ObservableCollection<PlayerViewModel> Players
    {
        get => _players;
        set
        {
            _players = value;
            OnPropertyChanged(nameof(Players));
            PresetIsModified();
        }
    }
    private Dictionary<string, PlayerViewModel> _lookupPlayers = []; 
    // na razie ciezko to rozwiazac bez mocnej ingerencji w logike whitelist przez to ze uuid jest opcjonalne
    // i trzeba by bylo przechwytywac aktualizowanie uuid z jego braku w playerze wiec obecnie nie ma z tego uzytku
    // chociaz fajnie bylo by to wykorzystac i uzywac zamiast GetPlayerByIGN() i wtedy, by to tez usprawnilo rzeczy z leaderboardem
    
    public string Name
    {
        get => _tournament.Name;
        set
        {
            _tournament.Name = value;
            OnPropertyChanged(nameof(Name));
            PresetIsModified();
        }
    }
    
    public bool IsAlwaysOnTop
    {
        get => _tournament.IsAlwaysOnTop;
        set
        {
            if (value == _tournament.IsAlwaysOnTop) return;
            
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
    public bool ShowOnlyLive
    {
        get => _tournament.ShowOnlyLive;
        set
        {
            _tournament.ShowOnlyLive = value;
            OnPropertyChanged(nameof(ShowOnlyLive));
            PresetIsModified();
        }
    }
    public bool AddUnknownPacemanPlayersToWhitelist
    {
        get => _tournament.AddUnknownPacemanPlayersToWhitelist;
        set
        {
            _tournament.AddUnknownPacemanPlayersToWhitelist = value;
            OnPropertyChanged(nameof(AddUnknownRankedPlayersToWhitelist));
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

    public Action<ControllerMode, bool>? OnControllerModeChanged;
    public ControllerMode ControllerMode
    {
        get => _tournament.ControllerMode;
        set
        {
            if (_tournament.ControllerMode == value) return;
            
            _tournament.ControllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
            PresetIsModified();

            if (value == ControllerMode.Ranked)
                ManagementData = new RankedManagementData();
            else if (value == ControllerMode.Paceman)
                ManagementData = new PacemanManagementData();
            else if (value == ControllerMode.Solo)
                ManagementData = new SoloManagementData();
            
            UpdateBackgroundService(value);
        }
    }
    
    public string RankedApiPlayerName
    {
        get => _tournament.RankedApiPlayerName;
        set
        {
            _tournament.RankedApiPlayerName = value;
            PresetIsModified();
            OnPropertyChanged(nameof(RankedApiPlayerName));
            UpdateBackgroundService(ControllerMode.Ranked);
        }
    }
    public string RankedApiKey
    {
        get => _tournament.RankedApiKey;
        set
        {
            _tournament.RankedApiKey = value;
            PresetIsModified();
            OnPropertyChanged(nameof(RankedApiKey));
            UpdateBackgroundService(ControllerMode.Ranked);
        }
    }
    public bool AddUnknownRankedPlayersToWhitelist
    {
        get => _tournament.AddUnknownRankedPlayersToWhitelist;
        set
        {
            _tournament.AddUnknownRankedPlayersToWhitelist = value;
            OnPropertyChanged(nameof(AddUnknownRankedPlayersToWhitelist));
            PresetIsModified();
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
        private set
        {
            _isPresetModified = value;
            OnPropertyChanged(nameof(IsPresetModified));
        }
    }
    
    public bool HasBeenRemoved { get; set; } = true;


    public void ChangeData(Tournament tournament)
    {
        if (tournament == null) return;
        _tournament = tournament;

        SetupPreset();
        
        IsCurrentlyOpened = true;
        HasBeenRemoved = false;
        
        _tournament.Leaderboard.Initialize();
        UpdateBackgroundService(ControllerMode);
        
        PresetIsSaved();
    }

    private void SetupPreset()
    {
        UpdatePlayers();
        UpdateGoodPacesTexts();
        
        RefreshUI();
        
        SetAlwaysOnTop();
    }
    
    public void RefreshUI()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsAlwaysOnTop));
        OnPropertyChanged(nameof(IsUsingTeamNames));
        OnPropertyChanged(nameof(IsUsingWhitelistOnPaceMan));
        OnPropertyChanged(nameof(ShowOnlyLive));
        OnPropertyChanged(nameof(AddUnknownPacemanPlayersToWhitelist));
        
        OnPropertyChanged(nameof(Port));
        OnPropertyChanged(nameof(Password));
        OnPropertyChanged(nameof(SceneCollection));
        OnPropertyChanged(nameof(FilterNameAtStartForSceneItems));
        
        OnPropertyChanged(nameof(SetPovHeadsInBrowser));
        OnPropertyChanged(nameof(SetPovPBText));
        OnPropertyChanged(nameof(DisplayedNameType));
        
        OnPropertyChanged(nameof(IsUsingTwitchAPI));
        OnPropertyChanged(nameof(ShowStreamCategory));
        
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
        
        OnPropertyChanged(nameof(RankedApiPlayerName));
        OnPropertyChanged(nameof(RankedApiKey));
        OnPropertyChanged(nameof(AddUnknownRankedPlayersToWhitelist));
    }
    public void UpdatePlayers()
    {
        Players.Clear();
        _lookupPlayers.Clear();
        foreach (var player in _tournament.Players)
        {
            var playerViewModel = new PlayerViewModel(player);
            playerViewModel.Initialize();
            playerViewModel.StreamData.LiveData.Clear(false);
            Players.Add(playerViewModel);
            AddPlayerToDictionary(playerViewModel);
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

    private void AddPlayerToDictionary(PlayerViewModel player)
    {
        if (string.IsNullOrEmpty(player.UUID)) return;
            
        try
        {
            _lookupPlayers.Add(player.UUID, player);
        } catch{/**/}
    }
    private void RemovePlayerFromDictionary(PlayerViewModel player)
    {
        if (string.IsNullOrEmpty(player.UUID)) return;
        _lookupPlayers.Remove(player.UUID);
    }
    
    public void AddPlayer(PlayerViewModel playerViewModel)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Add(playerViewModel);
            AddPlayerToDictionary(playerViewModel);
            _tournament.Players.Add(playerViewModel.Data);
            PresetIsModified();
        });
    }
    public void RemovePlayer(PlayerViewModel playerViewModel)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Players.Remove(playerViewModel);
            RemovePlayerFromDictionary(playerViewModel);
            _tournament.Players.Remove(playerViewModel.Data);
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
        
    public PlayerViewModel? GetPlayerByStreamName(string name, StreamType type)
    {
        if (string.IsNullOrEmpty(name)) return null;
        
        int n = Players.Count;
        for (int i = 0; i < n; i++)
        {
            var current = Players[i];
            if ((current.StreamData.ExistName(name) && type == StreamType.twitch) ||
                (current.StreamData.Other.Equals(name, StringComparison.OrdinalIgnoreCase) && current.StreamData.OtherType == type))
                return current;
        }
        return null;
    }
    public PlayerViewModel? GetPlayerByUUID(string uuid)
    {
        foreach (var player in Players)
        {
            if (!player.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase)) continue;
            return player;
        }
        
        return null;
    }
    public PlayerViewModel? GetPlayerByIGN(string ign)
    {
        foreach (var player in Players)
        {
            if (!player.InGameName!.Equals(ign, StringComparison.OrdinalIgnoreCase)) continue;
            return player;
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
        ShowOnlyLive = true;
        AddUnknownPacemanPlayersToWhitelist = false;
        
        Port = 4455;
        Password = string.Empty;
        SceneCollection = string.Empty;
        FilterNameAtStartForSceneItems = "pov";
    
        SetPovHeadsInBrowser = false;
        SetPovPBText = false;
        DisplayedNameType = DisplayedNameType.None;
    
        IsUsingTwitchAPI = false;
        ShowStreamCategory = true;

        PaceManRefreshRateMiliseconds = 3000;
    
        Structure2GoodPaceMiliseconds = 270000;
        FirstPortalGoodPaceMiliseconds = 360000;
        EnterStrongholdGoodPaceMiliseconds = 450000;
        EnterEndGoodPaceMiliseconds = 480000;
        CreditsGoodPaceMiliseconds = 600000;
        
        ControllerMode = ControllerMode.None;
        
        RankedApiPlayerName = string.Empty;
        RankedApiKey = string.Empty;
        AddUnknownRankedPlayersToWhitelist = false;
        
        PresetIsModified();
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

    private void UpdateBackgroundService(ControllerMode mode)
    {
        //TODO: 3 Przeniesc wszystkie fieldy do presetmanager usuwaj tutaj baseviewmodel i validacje danych, bo jest to idiotycznie rozwiazane obecnie
        ClearErrors(nameof(RankedApiKey));
        ClearErrors(nameof(RankedApiPlayerName));

        bool isValidated = true;
        switch (mode)
        {
            case ControllerMode.Ranked:
                if (string.IsNullOrEmpty(RankedApiKey))
                {
                    AddError(nameof(RankedApiKey), "Ranked api cannot be empty");
                    isValidated = false;
                }
                if (string.IsNullOrEmpty(RankedApiPlayerName))
                {
                    AddError(nameof(RankedApiPlayerName), "Ranked player name for api cannot be empty");
                    isValidated = false;
                }
                break;
            case ControllerMode.Paceman:
                break;
        }
        
        OnControllerModeChanged?.Invoke(mode, isValidated);
    }
    
    public void PresetIsModified()
    {
        IsPresetModified = true;
    }
    public void PresetIsSaved()
    {
        IsPresetModified = false;
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        return _errors!.GetValueOrDefault(propertyName)!;
    }
    protected void AddError(string propertyName, string error)
    {
        if (!_errors.ContainsKey(propertyName)) _errors[propertyName] = [];
        if (_errors[propertyName].Contains(error)) return;
        
        _errors[propertyName].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
    protected void ClearErrors(string propertyName)
    {
        if (!_errors.Remove(propertyName)) return;
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
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