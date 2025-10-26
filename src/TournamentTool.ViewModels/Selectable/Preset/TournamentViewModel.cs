using System.Collections;
using System.ComponentModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.ViewModels.Selectable.Preset;

public class TournamentViewModel : BaseViewModel, INotifyDataErrorInfo
{
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly ITournamentState _tournamentState;
    private readonly IBackgroundCoordinator _backgroundCoordinator;
    private readonly Dictionary<string, List<string>> _errors = [];
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _errors.Count != 0;
    
    public string Name
    {
        get => _tournamentState.CurrentPreset.Name;
        set
        {
            _tournamentState.CurrentPreset.Name = value;
            OnPropertyChanged(nameof(Name));
            PresetIsModified();
        }
    }
    
    public bool IsUsingTeamNames
    {
        get => _tournamentState.CurrentPreset.IsUsingTeamNames;
        set
        {
            if (_tournamentState.CurrentPreset.IsUsingTeamNames == value) return;
                
            _tournamentState.CurrentPreset.IsUsingTeamNames = value;
            OnPropertyChanged(nameof(IsUsingTeamNames));
            PresetIsModified();
            _playerRepository.UpdateTeamNamesForPlayers();
        }
    }
    public bool IsUsingWhitelistOnPaceMan
    {
        get => _tournamentState.CurrentPreset.IsUsingWhitelistOnPaceMan;
        set
        {
            _tournamentState.CurrentPreset.IsUsingWhitelistOnPaceMan = value;
            OnPropertyChanged(nameof(IsUsingWhitelistOnPaceMan));
            PresetIsModified();
        }
    }
    public bool ShowOnlyLive
    {
        get => _tournamentState.CurrentPreset.ShowOnlyLive;
        set
        {
            _tournamentState.CurrentPreset.ShowOnlyLive = value;
            OnPropertyChanged(nameof(ShowOnlyLive));
            PresetIsModified();
        }
    }
    public bool AddUnknownPacemanPlayersToWhitelist
    {
        get => _tournamentState.CurrentPreset.AddUnknownPacemanPlayersToWhitelist;
        set
        {
            _tournamentState.CurrentPreset.AddUnknownPacemanPlayersToWhitelist = value;
            OnPropertyChanged(nameof(AddUnknownRankedPlayersToWhitelist));
            PresetIsModified();
        }
    }
    
    public string SceneCollection
    {
        get => _tournamentState.CurrentPreset.SceneCollection;
        set
        {
            _tournamentState.CurrentPreset.SceneCollection = value;
            OnPropertyChanged(nameof(SceneCollection));
            PresetIsModified();
        }
    }
    
    public bool IsUsingTwitchAPI
    {
        get => _tournamentState.CurrentPreset.IsUsingTwitchAPI;
        set
        {
            if (_tournamentState.CurrentPreset.IsUsingTwitchAPI == value) return;
                
            _tournamentState.CurrentPreset.IsUsingTwitchAPI = value;
            OnPropertyChanged(nameof(IsUsingTwitchAPI));
            PresetIsModified();
            _playerRepository.UpdateCategoryForPlayers();
        }
    }
    public bool ShowStreamCategory
    {
        get => _tournamentState.CurrentPreset.ShowStreamCategory;
        set
        {
            if (_tournamentState.CurrentPreset.ShowStreamCategory == value) return;
                
            _tournamentState.CurrentPreset.ShowStreamCategory = value;
            OnPropertyChanged(nameof(ShowStreamCategory));
            PresetIsModified();
            _playerRepository.UpdateCategoryForPlayers();
        }
    }
    
    public bool SetPovHeadsInBrowser
    {
        get => _tournamentState.CurrentPreset.SetPovHeadsInBrowser;
        set
        {
            _tournamentState.CurrentPreset.SetPovHeadsInBrowser = value;
            OnPropertyChanged(nameof(SetPovHeadsInBrowser));
            PresetIsModified();
        }
    }
    public bool SetPovPBText
    {
        get => _tournamentState.CurrentPreset.SetPovPBText;
        set
        {
            _tournamentState.CurrentPreset.SetPovPBText = value;
            OnPropertyChanged(nameof(SetPovPBText));
            PresetIsModified();
        }
    }
    public DisplayedNameType DisplayedNameType
    {
        get => _tournamentState.CurrentPreset.DisplayedNameType;
        set
        {
            _tournamentState.CurrentPreset.DisplayedNameType = value;
            OnPropertyChanged(nameof(DisplayedNameType));
            PresetIsModified();
        }
    }
    
    public int PaceManRefreshRateMiliseconds
    {
        get => _tournamentState.CurrentPreset.PaceManRefreshRateMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.PaceManRefreshRateMiliseconds = value < 3000 ? 3000 : value;
            OnPropertyChanged(nameof(PaceManRefreshRateMiliseconds));
            PresetIsModified();
        }
    }
    
    public int Structure2GoodPaceMiliseconds
    {
        get => _tournamentState.CurrentPreset.Structure2GoodPaceMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.Structure2GoodPaceMiliseconds = value;
    
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
        get => _tournamentState.CurrentPreset.FirstPortalGoodPaceMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.FirstPortalGoodPaceMiliseconds = value;
    
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
        get => _tournamentState.CurrentPreset.EnterStrongholdGoodPaceMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.EnterStrongholdGoodPaceMiliseconds = value;
    
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
        get => _tournamentState.CurrentPreset.EnterEndGoodPaceMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.EnterEndGoodPaceMiliseconds = value;
    
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
        get => _tournamentState.CurrentPreset.CreditsGoodPaceMiliseconds;
        set
        {
            _tournamentState.CurrentPreset.CreditsGoodPaceMiliseconds = value;
    
            string time = TimeSpan.FromMilliseconds(value).ToString(@"mm\:ss");
            CreditsToText = $"Finish (sub {time})";
            OnPropertyChanged(nameof(CreditsToText));
            OnPropertyChanged(nameof(CreditsGoodPaceMiliseconds));
            PresetIsModified();
        }
    }
    public string? CreditsToText { set; get; }

    public ControllerMode ControllerMode
    {
        get => _tournamentState.CurrentPreset.ControllerMode;
        set
        {
            if (_tournamentState.CurrentPreset.ControllerMode == value) return;
            
            _tournamentState.CurrentPreset.ControllerMode = value;
            OnPropertyChanged(nameof(ControllerMode));
            PresetIsModified();

            if (value == ControllerMode.Ranked)
                _tournamentState.CurrentPreset.ManagementData = new RankedManagementData();
            else if (value == ControllerMode.Paceman)
                _tournamentState.CurrentPreset.ManagementData = new PacemanManagementData();
            else if (value == ControllerMode.Solo)
                _tournamentState.CurrentPreset.ManagementData = new SoloManagementData();
            
            UpdateBackgroundService(value);
        }
    }
    
    public string RankedApiPlayerName
    {
        get => _tournamentState.CurrentPreset.RankedApiPlayerName;
        set
        {
            _tournamentState.CurrentPreset.RankedApiPlayerName = value;
            PresetIsModified();
            OnPropertyChanged(nameof(RankedApiPlayerName));
            UpdateBackgroundService(ControllerMode.Ranked);
        }
    }
    public string RankedApiKey
    {
        get => _tournamentState.CurrentPreset.RankedApiKey;
        set
        {
            _tournamentState.CurrentPreset.RankedApiKey = value;
            PresetIsModified();
            OnPropertyChanged(nameof(RankedApiKey));
            UpdateBackgroundService(ControllerMode.Ranked);
        }
    }
    public bool AddUnknownRankedPlayersToWhitelist
    {
        get => _tournamentState.CurrentPreset.AddUnknownRankedPlayersToWhitelist;
        set
        {
            _tournamentState.CurrentPreset.AddUnknownRankedPlayersToWhitelist = value;
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

    
    public TournamentViewModel(ITournamentPlayerRepository playerRepository, ITournamentState tournamentState, IBackgroundCoordinator backgroundCoordinator, 
        IDispatcherService dispatcher) : base(dispatcher)
    {
        _playerRepository = playerRepository;
        _tournamentState = tournamentState;
        _backgroundCoordinator = backgroundCoordinator;

        _tournamentState.PresetChanged += OnPresetChanged;
    }
    public override void Dispose()
    {
        _tournamentState.PresetChanged -= OnPresetChanged;
    }
    
    private void OnPresetChanged(object? sender, Tournament? tournament)
    {
        if (tournament == null)
        {
            IsCurrentlyOpened = false;
            return;
        }
        IsCurrentlyOpened = true;
        
        //update background service bedzie raczej w servisie do controllermode i tak samo leaderboard initialize
        if (_tournamentState.CurrentPreset.ManagementData == null)
        {
            if (ControllerMode == ControllerMode.Ranked)
                _tournamentState.CurrentPreset.ManagementData = new RankedManagementData();
            else if (ControllerMode == ControllerMode.Paceman)
                _tournamentState.CurrentPreset.ManagementData = new PacemanManagementData();
            else if (ControllerMode == ControllerMode.Solo)
                _tournamentState.CurrentPreset.ManagementData = new SoloManagementData();
        }
        UpdateBackgroundService(_tournamentState.CurrentPreset.ControllerMode);
        
        PaceManRefreshRateMiliseconds = _tournamentState.CurrentPreset.PaceManRefreshRateMiliseconds;
        RefreshUI();
        UpdateGoodPacesTexts();
    }

    public void RefreshUI()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(IsUsingTeamNames));
        OnPropertyChanged(nameof(IsUsingWhitelistOnPaceMan));
        OnPropertyChanged(nameof(ShowOnlyLive));
        OnPropertyChanged(nameof(AddUnknownPacemanPlayersToWhitelist));
        
        OnPropertyChanged(nameof(SceneCollection));
        
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

    private void UpdateBackgroundService(ControllerMode mode)
    {
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
        
        _backgroundCoordinator.Initialize(mode, isValidated);
    }
    
    private void PresetIsModified()
    {
        _tournamentState.MarkAsModified();
    }
    
    public void Clear()
    {
        IsUsingTeamNames = false;
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
        
        PresetIsModified();
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
}