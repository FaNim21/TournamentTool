using System.Windows.Input;
using TournamentTool.Commands;
using TournamentTool.Interfaces;
using TournamentTool.ViewModels.Settings;

namespace TournamentTool.ViewModels.Selectable;

public class SettingsViewModel : SelectableViewModel
{
    public List<ISettingsTab> Tabs { get; } = [];

    private ISettingsTab? _currentTab;
    public ISettingsTab? CurrentTab
    {
        get => _currentTab;
        set
        {
            _currentTab = value;
            OnPropertyChanged(nameof(CurrentTab));
        }
    }

    public ICommand ChangeTabCommand { get; private set; }


    public SettingsViewModel(ICoordinator coordinator, ISettings settingsService) : base(coordinator)
    {
        ChangeTabCommand = new RelayCommand<string>(ChangeTab);
        
        var general = new GeneralTabViewModel(settingsService.Settings);
        var hotkeys = new HotkeysTabViewModel(settingsService.Settings);
        var keys = new APIKeysTabViewModel(settingsService.APIKeys);
        
        Tabs.Add(general);
        Tabs.Add(hotkeys);
        Tabs.Add(keys);

        ChangeTab("general");
    }

    public void ChangeTab(string name)
    {
        if (CurrentTab != null && CurrentTab.Name.Equals(name)) return;
        
        foreach (var tab in Tabs)
        {
            if (!tab.Name.Equals(name)) continue;
            
            CurrentTab?.OnClose();
            CurrentTab = tab;
            CurrentTab.OnOpen();
            OnPropertyChanged(nameof(CurrentTab));
        }
    }
}
