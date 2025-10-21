using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Interfaces;
using TournamentTool.ViewModels.Commands;
using TournamentTool.ViewModels.Settings;

namespace TournamentTool.ViewModels.Selectable;

public class SettingsViewModel : SelectableViewModel
{
    private readonly ISettingsSaver _settingsSaver;

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


    public SettingsViewModel(ICoordinator coordinator, ISettings settingsService, ISettingsSaver settingsSaver, IDispatcherService dispatcher, IWindowService windowService, IInputController inputController, IDialogService dialogService) : base(coordinator, dispatcher)
    {
        _settingsSaver = settingsSaver;

        ChangeTabCommand = new RelayCommand<string>(ChangeTab);
        
        var general = new GeneralTabViewModel(settingsService.Settings, windowService, dispatcher);
        var hotkeys = new HotkeysTabViewModel(settingsService.Settings, dispatcher, inputController);
        var keys = new APIKeysTabViewModel(settingsService.APIKeys, dispatcher, dialogService);
        
        Tabs.Add(general);
        Tabs.Add(hotkeys);
        Tabs.Add(keys);

        ChangeTab("general");
    }
    public override bool OnDisable()
    {
        _settingsSaver.Save();
        return true;
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
