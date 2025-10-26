using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public class StatusBarViewModel : BaseViewModel
{
    private readonly ITournamentState _tournamentState;
    
    private string _presetName = string.Empty;
    public string PresetName
    {
        get => _presetName;
        set
        {
            if (_presetName == value) return;
            
            _presetName = value;
            OnPropertyChanged(nameof(PresetName));
        }
    }

    private bool _isPresetModified;
    public bool IsPresetModified
    {
        get => _isPresetModified;
        set
        {
            if (_isPresetModified == value) return;
            
            _isPresetModified = value;
            OnPropertyChanged(nameof(IsPresetModified));
        }
    }

    public ObservableCollection<StatusItemViewModel> StatusItems { get; } = [];

    
    public StatusBarViewModel(ObsController obs, ITwitchService twitch, IBackgroundCoordinator backgroundCoordinator, IDispatcherService dispatcher, 
        ITournamentState tournamentState, NotificationPanelViewModel notificationPanelViewModel, IImageService imageService, IMenuService menuService) : base(dispatcher)
    {
        _tournamentState = tournamentState;

        var notifications = new NotificationStatusViewModel(notificationPanelViewModel, dispatcher, imageService, menuService);
        var obsStatus = new OBSStatusViewModel(obs, dispatcher, imageService, menuService);
        var backgroundServiceStatus = new BackgroundServiceStatusViewModel(backgroundCoordinator, (IBackgroundServiceRegistry)backgroundCoordinator, dispatcher, imageService, menuService);
        var twitchStatus = new TwitchStatusViewModel(twitch, dispatcher, imageService, menuService);

        StatusItems.Add(notifications);
        StatusItems.Add(obsStatus);
        StatusItems.Add(twitchStatus);
        StatusItems.Add(backgroundServiceStatus);
        
        _tournamentState.PresetChanged += OnPresetChanged;
        _tournamentState.PresetNameChanged += OnPresetNameChanged;
        _tournamentState.ModificationStateChanged += OnPresetModified;
    }
    public override void Dispose()
    {
        _tournamentState.PresetChanged -= OnPresetChanged;
        _tournamentState.PresetNameChanged -= OnPresetNameChanged;
        _tournamentState.ModificationStateChanged -= OnPresetModified;
        
        foreach (var item in StatusItems)
        {
            item.Dispose();
        }
    }
    
    private void OnPresetChanged(object? sender, Tournament? tournament)
    {
        if (tournament == null)
        {
            PresetName = string.Empty;
            return;
        }
        
        PresetName = tournament.Name;
    }
    private void OnPresetNameChanged(object? sender, string newName)
    {
        PresetName = newName;
    }
    private void OnPresetModified(object? sender, bool isModified)
    {
        IsPresetModified = isModified;
    }
}