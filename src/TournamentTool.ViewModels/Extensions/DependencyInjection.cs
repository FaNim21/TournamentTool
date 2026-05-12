using Microsoft.Extensions.DependencyInjection;
using TournamentTool.Core.Common;
using TournamentTool.Core.Factories;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Factories;
using TournamentTool.ViewModels.Logging;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.StatusBar;

namespace TournamentTool.ViewModels.Extensions;

public static class DependencyInjection
{
    public static void AddViewModels(this IServiceCollection services)
    {
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));
        services.AddSingleton<MainViewModel>();
        
        services.AddSingleton<StatusBarViewModel>();
        
        //View model factories
        services.AddSingleton<IPlayerViewModelFactory, PlayerViewModelFactory>();
        services.AddSingleton<IPopupViewModelFactory, PopupViewModelFactory>();
        services.AddSingleton<ISceneControllerViewModelFactory, SceneCanvasViewModelFactory>();

        //Logging control
        services.AddSingleton<NotificationPanelViewModel>();
        services.AddSingleton<ConsoleViewModel>();
        
        //main navigation panels
        services.AddSingleton<ControllerViewModel>();           //To powinno zostac singletonem, ale lepiej i tak trzeba przekminic zastosowanie tego
        services.AddTransient<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>();        //zanim transient to trzeba bedzie zrobic serwis, ktory trzyma info o eventach pacemanowych, bo sa w ctor
        services.AddTransient<LeaderboardPanelViewModel>();
        services.AddTransient<SceneManagementViewModel>();
        services.AddTransient<UpdatesViewModel>();
        services.AddTransient<SettingsViewModel>();
    }
}