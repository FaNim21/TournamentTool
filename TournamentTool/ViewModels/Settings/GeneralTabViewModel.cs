using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.Models;

namespace TournamentTool.ViewModels.Settings;

public class GeneralTabViewModel : BaseViewModel, ISettingsTab
{
    private readonly Models.Settings _settings;

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
            
            Application.Current?.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.Topmost = _settings.IsAlwaysOnTop;
                }
            });
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

    
    public GeneralTabViewModel(Models.Settings settings)
    {
        Name = "general";
        _settings = settings;
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