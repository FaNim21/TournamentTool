using System.IO;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TournamentTool.Factories;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Modules;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.External;
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

        services.AddSingleton<IPlayerViewModelFactory, PlayerViewModelFactory>();

        services.AddHttpClient<IMinecraftDataService, MinecraftDataService>();
        services.AddHttpClient<IPacemanAPIService, PacemanAPIService>();
        services.AddHttpClient<IRankedAPIService, RankedAPIService>();

        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<LogStore>();
        services.AddSingleton<NotificationPanelViewModel>();
        
        services.AddSingleton<TournamentViewModel>();
        services.AddSingleton<IPresetInfo>(s => s.GetRequiredService<TournamentViewModel>());
        
        services.AddSingleton<ObsController>();
        
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISettings>(s => s.GetRequiredService<SettingsService>());
        services.AddSingleton<ISettingsSaver>(s => s.GetRequiredService<SettingsService>());
        
        services.AddSingleton<IPresetSaver, PresetService>();
        services.AddSingleton<TwitchService>();
        
        services.AddSingleton<ILeaderboardManager, LeaderboardManager>();
        services.AddSingleton<ILuaScriptsManager, LuaScriptsManager>();

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
        Directory.CreateDirectory(Consts.PresetsPath);
        Directory.CreateDirectory(Consts.LogsPath);
        Directory.CreateDirectory(Consts.ScriptsPath);
        Directory.CreateDirectory(Consts.LeaderboardScriptsPath);
        
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settingsService.Load();

        mainViewModel.NavigationService.Startup(mainViewModel);
        mainWindow.Show();

        InputController.Instance.Initialize();
        mainViewModel.HotkeySetup();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var settings = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settings.Save();
        
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}