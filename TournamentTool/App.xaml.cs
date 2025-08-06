using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Modules;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.StatusBar;

namespace TournamentTool;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    
    
    public App()
    {
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<LogStore>();
        services.AddSingleton<NotificationPanelViewModel>();
        
        services.AddSingleton<TournamentViewModel>();
        services.AddSingleton<IPresetSaver, PresetService>();
        
        services.AddSingleton<ILeaderboardManager, LeaderboardManager>();
        services.AddSingleton<ILuaScriptsManager, LuaScriptsManager>();
        
        services.AddSingleton<ObsController>();
        services.AddSingleton<TwitchService>();

        services.AddSingleton<StatusBarViewModel>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ICoordinator, MainViewModelCoordinator>();
        
        services.AddSingleton<IBackgroundCoordinator, BackgroundCoordinator>();
        
        services.AddSingleton<ControllerViewModel>();
        services.AddSingleton<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>();
        services.AddTransient<LeaderboardPanelViewModel>();
        services.AddTransient<SceneManagementViewModel>();
        services.AddSingleton<UpdatesViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));

        _serviceProvider = services.BuildServiceProvider();

        var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
        LogService.Initialize(loggingService);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var navigationService = _serviceProvider.GetService<INavigationService>();

        mainViewModel.NavigationService.Startup(mainViewModel);
        mainWindow.Show();

        InputController.Instance.Initialize();
        mainViewModel.HotkeySetup();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
