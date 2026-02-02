using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using TournamentTool.App.Services;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Configuration;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.State;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities.Player;
using TournamentTool.ViewModels.Logging;
using TournamentTool.ViewModels.Menu;
using TournamentTool.ViewModels.Selectable;
using TournamentTool.ViewModels.StatusBar;

namespace TournamentTool.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    
    
    public App()
    {
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        IServiceCollection services = new ServiceCollection();

        services.AddHttpClient();
        services.Configure<HttpClientFactoryOptions>(options =>
        {
            options.HttpClientActions.Add(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", $"TournamentTool/{Consts.Version}");
                client.Timeout = TimeSpan.FromSeconds(10); // globalny timeout
            });
        }); 
        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        //App services implementations
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IApplicationState, ApplicationState>();
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IInputController, InputController>();
        services.AddSingleton<IDataProtect, WindowsDataProtect>();
        services.AddSingleton<IMenuService, MenuService>();
        services.AddSingleton<IUIInteractionService, UIInteractionService>();
        
        //View model factories
        services.AddSingleton<IPlayerViewModelFactory, PlayerViewModelFactory>();
        
        //Http access services
        services.AddSingleton<IMinecraftDataService, MinecraftDataService>();
        services.AddSingleton<IPacemanAPIService, PacemanAPIService>();
        services.AddSingleton<IRankedAPIService, RankedAPIService>();
        services.AddSingleton<IImageService, ImageService>();
        services.AddSingleton<IUpdateCheckerService, UpdateCheckerService>();

        //Logging control
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ILogStore, LogStore>();
        services.AddSingleton<NotificationPanelViewModel>();
        services.AddSingleton<ConsoleViewModel>();
        
        //Tournament preset control
        services.AddSingleton<ITournamentState, TournamentState>();
        services.AddSingleton<ITournamentPresetManager, TournamentPresetManager>();
        services.AddSingleton<ITournamentPlayerRepository, TournamentPlayerRepository>();
        services.AddSingleton<ITournamentLeaderboardRepository, TournamentLeaderboardRepository>();
        
        //Rest xd
        services.AddSingleton<ObsController>();
        
        //Settings
        services.AddSingleton<SettingsProvider>();
        services.AddSingleton<ISettingsProvider>(s => s.GetRequiredService<SettingsProvider>());
        services.AddSingleton<ISettingsSaver>(s => s.GetRequiredService<SettingsProvider>());
        
        services.AddSingleton<IPresetSaver, PresetService>();
        services.AddSingleton<ITwitchService, TwitchService>();
        
        services.AddSingleton<ILeaderboardManager, LeaderboardManager>();
        services.AddSingleton<ILuaScriptsManager, LuaScriptsManager>();

        services.AddSingleton<StatusBarViewModel>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IBackgroundCoordinator, BackgroundCoordinator>();
        
        //main navigation panels
        services.AddSingleton<ControllerViewModel>();
        services.AddTransient<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>(); //zanim transient to trzeba bedzie zrobic serwis, ktory trzyma info o eventach pacemanowych, bo sa w ctor
        services.AddTransient<LeaderboardPanelViewModel>();
        services.AddTransient<SceneManagementViewModel>();
        services.AddTransient<UpdatesViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));
        
        services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();

        _serviceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Directory.CreateDirectory(Consts.PresetsPath);
        Directory.CreateDirectory(Consts.LogsPath);
        Directory.CreateDirectory(Consts.ScriptsPath);
        Directory.CreateDirectory(Consts.LeaderboardScriptsPath);
        
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
        
        ISettingsSaver settingsSaver = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settingsSaver.Load();
        
        var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
        LogHelper.Initialize(loggingService);
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        IApplicationLifetime applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();
        applicationLifetime.OnStartup();
        
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        var startupSelectable = _serviceProvider.GetRequiredService<PresetManagerViewModel>();
        navigationService.Startup(startupSelectable);
        
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IApplicationLifetime applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();
        applicationLifetime.OnExit();
        
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}