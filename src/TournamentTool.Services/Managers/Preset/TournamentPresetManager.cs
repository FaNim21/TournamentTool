using System.Collections.ObjectModel;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;

namespace TournamentTool.Services.Managers.Preset;

/// <summary>
/// TODO: 0 PODZIELIC TO NA WIECEJ SERWISOW I MIEC LEPSZA KONTROLE Z ZEWNATRZ
/// - Player repository?
/// - Controller mode service?
/// -
/// </summary>
public class TournamentPresetManager : ITournamentPresetManager
{
    private readonly IPresetSaver _presetSaver;
    private readonly IDialogService _dialogService;
    
    
    private Tournament _tournament = new();

    //public ReadOnlyObservableCollection<IPlayerViewModel> Players = [];
    //public ObservableCollection<IPlayerViewModel> Players { get; } = [];
    // /\ to dziala ogolnie i mysle ze trzeba pojsc ta droga

    public string Name
    {
        get => _tournament.Name;
        set => _tournament.Name = value;
    }

    /*public ControllerMode ControllerMode
    {
        get => _tournament.ControllerMode;
        set => _tournament.ControllerMode = value;
    }*/

    public bool IsCurrentlyOpened { get; private set; }
    public event Action<ControllerMode, bool>? OnControllerModeChanged;
    public bool IsPresetModified { get; private set; }
    public bool HasBeenRemoved { get; set; } = true;

    public event Action<Tournament>? OnPresetChanged;
    

    public TournamentPresetManager(IPresetSaver presetSaver, IDialogService dialogService)
    {
        _presetSaver = presetSaver;
        _dialogService = dialogService;
    }
    
    public void ChangeData(Tournament? tournament)
    {
        if (tournament == null)
        {
            IsCurrentlyOpened = false;
            _tournament = new Tournament();
            return;
        }
        
        _tournament = tournament;

        // SetupPreset();
        
        IsCurrentlyOpened = true;
        HasBeenRemoved = false;
        
        _tournament.Leaderboard.Initialize();
        // UpdateBackgroundService(ControllerMode);
        
        OnPresetChanged?.Invoke(tournament);
        PresetIsSaved();
    }

    public void ChangeName(string newName)
    {
        if (!IsCurrentlyOpened) return;
        
        Name = newName;
        _presetSaver!.SavePreset();
    }

    public void PresetIsModified()
    {
        IsPresetModified = true;
    }
    public void PresetIsSaved()
    {
        IsPresetModified = false;
    }
    
    public bool IsNullOrEmpty()
    {
        return _tournament == null || HasBeenRemoved;
    }
    
    public Tournament GetData() => _tournament;

    public void Delete()
    {
        HasBeenRemoved = true;
        IsCurrentlyOpened = false;
    }
    public void Clear()
    {
        var result = _dialogService.Show($"Are you sure you want to clear all data in preset: {Name}", "Clearing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        
        ClearAllData();
    }
    private void ClearAllData()
    {
        /*IsUsingTeamNames = false;
        IsUsingWhitelistOnPaceMan = true;
        ShowOnlyLive = true;
        AddUnknownPacemanPlayersToWhitelist = false;
        
        SceneCollection = string.Empty;
    
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
        
        PresetIsModified();*/
    }
}