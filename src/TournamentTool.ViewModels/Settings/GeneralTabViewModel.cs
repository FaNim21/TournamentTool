using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Interfaces;

namespace TournamentTool.ViewModels.Settings;

public class GeneralTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly Domain.Entities.Settings _settings;
    private readonly IWindowService _windowService;

    public bool IsChosen { get; private set; }
    public string Name { get; }

    public int Port
    {
        get => _settings.Port;
        set
        {
            _settings.Port = value;
            OnPropertyChanged();
        }
    }
    public string Password
    {
        get => _settings.Password;
        set
        {
            _settings.Password = value;
            OnPropertyChanged();
        }
    }
    public string FilterNameAtStartForSceneItems
    {
        get => _settings.FilterNameAtStartForSceneItems;
        set
        {
            if (!value.StartsWith("head", StringComparison.OrdinalIgnoreCase))
                _settings.FilterNameAtStartForSceneItems = value;
    
            OnPropertyChanged();
        }
    }
    
    public bool IsAlwaysOnTop
    {
        get => _settings.IsAlwaysOnTop;
        set
        {
            if (value == _settings.IsAlwaysOnTop) return;
            
            _settings.IsAlwaysOnTop = value;
            OnPropertyChanged();
            
            _windowService.SetMainWindowTopMost(value);
        }
    }
    public bool SaveTwitchToken
    {
        get => _settings.SaveTwitchToken;
        set
        {
            _settings.SaveTwitchToken = value;
            OnPropertyChanged();
        }
    }
    public bool AutoLoginToTwitch
    {
        get => _settings.AutoLoginToTwitch;
        set
        {
            _settings.AutoLoginToTwitch = value;
            OnPropertyChanged();
        }
    }
    public bool SaveRankedPrivRoomDataOnSeedFinish
    {
        get => _settings.SaveRankedPrivRoomDataOnSeedFinish;
        set
        {
            _settings.SaveRankedPrivRoomDataOnSeedFinish = value;
            OnPropertyChanged();
        }
    }
    
    public HeadAPIType HeadAPIType
    {
        get => _settings.HeadAPIType;
        set
        {
            _settings.HeadAPIType = value;
            OnPropertyChanged();
        }
    }
    public string RankedApiDomain
    {
        get => _settings.RankedApiDomain;
        set
        {
            _settings.RankedApiDomain = value;
            OnPropertyChanged();
        }
    }

    public bool SaveLogsAfterShutdown
    {
        get => _settings.SaveLogsAfterShutdown;
        set
        {
            _settings.SaveLogsAfterShutdown = value;
            OnPropertyChanged();
        }
    }
    public int ConsoleLogsLimit
    {
        get => _settings.ConsoleLogsLimit;
        set
        {
            int clampedValue = Math.Clamp(value, 50, 500);
            _settings.ConsoleLogsLimit = clampedValue;
            OnPropertyChanged();
        }
    }
    
    public GeneralTabViewModel(Domain.Entities.Settings settings, IWindowService windowService, IDispatcherService dispatcher) : base(dispatcher)
    {
        Name = "general";
        
        _settings = settings;
        _windowService = windowService;
    }
    public void OnOpen()
    {
        IsChosen = true;
    }
    public void OnClose()
    {
        IsChosen = true;
    }
}