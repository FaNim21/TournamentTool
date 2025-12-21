using System.Windows.Input;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
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


    public SettingsViewModel(ICoordinator coordinator, ISettingsProvider settingsProvider, ISettingsSaver settingsSaver, IDispatcherService dispatcher, IWindowService windowService, IInputController inputController, IDialogService dialogService) : base(coordinator, dispatcher)
    {
        _settingsSaver = settingsSaver;

        ChangeTabCommand = new RelayCommand<string>(ChangeTab);
        
        Domain.Entities.Settings settings = settingsProvider.Get<Domain.Entities.Settings>();
        APIKeys apiKeys = settingsProvider.Get<APIKeys>();
        
        var general = new GeneralTabViewModel(settings, windowService, dispatcher);
        var hotkeys = new HotkeysTabViewModel(settings, dispatcher, inputController);
        var keys = new APIKeysTabViewModel(apiKeys, dispatcher, dialogService);
        
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
