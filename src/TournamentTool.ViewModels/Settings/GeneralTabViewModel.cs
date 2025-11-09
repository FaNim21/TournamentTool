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
            OnPropertyChanged(nameof(Port));
        }
    }
    public string Password
    {
        get => _settings.Password;
        set
        {
            _settings.Password = value;
            OnPropertyChanged(nameof(Password));
        }
    }
    public string FilterNameAtStartForSceneItems
    {
        get => _settings.FilterNameAtStartForSceneItems;
        set
        {
            if (!value.StartsWith("head", StringComparison.OrdinalIgnoreCase))
                _settings.FilterNameAtStartForSceneItems = value;
    
            OnPropertyChanged(nameof(FilterNameAtStartForSceneItems));
        }
    }
    
    public bool IsAlwaysOnTop
    {
        get => _settings.IsAlwaysOnTop;
        set
        {
            if (value == _settings.IsAlwaysOnTop) return;
            
            _settings.IsAlwaysOnTop = value;
            OnPropertyChanged(nameof(IsAlwaysOnTop));
            
            _windowService.SetMainWindowTopMost(value);
        }
    }
    public bool SaveTwitchToken
    {
        get => _settings.SaveTwitchToken;
        set
        {
            _settings.SaveTwitchToken = value;
            OnPropertyChanged(nameof(SaveTwitchToken));
        }
    }

    public HeadAPIType HeadAPIType
    {
        get => _settings.HeadAPIType;
        set
        {
            _settings.HeadAPIType = value;
            OnPropertyChanged(nameof(HeadAPIType));
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