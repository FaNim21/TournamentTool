using System.Collections.ObjectModel;
using TournamentTool.Interfaces;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.ViewModels.StatusBar;

public class StatusBarViewModel : BaseViewModel
{
    public IPresetInfo PresetInfo { get; }
    public ObservableCollection<StatusItemViewModel> StatusItems { get; } = [];

    
    public StatusBarViewModel(IPresetInfo presetInfo, ObsController obs, TwitchService twitch, IBackgroundCoordinator backgroundCoordinator, NotificationPanelViewModel notificationPanelViewModel)
    {
        PresetInfo = presetInfo;

        var notifications = new NotificationStatusViewModel(notificationPanelViewModel);
        var obsStatus = new OBSStatusViewModel(obs);
        var backgroundServiceStatus = new BackgroundServiceStatusViewModel(backgroundCoordinator, (IBackgroundServiceRegistry)backgroundCoordinator);
        var twitchStatus = new TwitchStatusViewModel(twitch);

        StatusItems.Add(notifications);
        StatusItems.Add(obsStatus);
        StatusItems.Add(twitchStatus);
        StatusItems.Add(backgroundServiceStatus);
    } 
    
    public override void Dispose()
    {
        foreach (var item in StatusItems)
        {
            item.Dispose();
        }
    }
}