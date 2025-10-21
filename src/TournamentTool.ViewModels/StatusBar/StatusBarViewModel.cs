using System.Collections.ObjectModel;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Controllers;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.ViewModels.StatusBar;

public class StatusBarViewModel : BaseViewModel
{
    public ObservableCollection<StatusItemViewModel> StatusItems { get; } = [];

    
    public StatusBarViewModel(ObsController obs, TwitchService twitch, IBackgroundCoordinator backgroundCoordinator, IDispatcherService dispatcher, 
        NotificationPanelViewModel notificationPanelViewModel, IImageService imageService, IMenuService menuService) : base(dispatcher)
    {
        var notifications = new NotificationStatusViewModel(notificationPanelViewModel, dispatcher, imageService, menuService);
        var obsStatus = new OBSStatusViewModel(obs, dispatcher, imageService, menuService);
        var backgroundServiceStatus = new BackgroundServiceStatusViewModel(backgroundCoordinator, (IBackgroundServiceRegistry)backgroundCoordinator, dispatcher, imageService, menuService);
        var twitchStatus = new TwitchStatusViewModel(twitch, dispatcher, imageService, menuService);

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