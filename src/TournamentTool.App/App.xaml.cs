using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using TournamentTool.App.Components;
using TournamentTool.App.Services;
using TournamentTool.Core.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Services.Controllers;
using TournamentTool.Services.Coordinators;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.State;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities.Player;
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
        services.AddSingleton<LogStore>();
        services.AddSingleton<NotificationPanelViewModel>();
        
        //Tournament preset control
        services.AddSingleton<ITournamentState, TournamentState>();
        services.AddSingleton<ITournamentPresetManager, TournamentPresetManager>();
        services.AddSingleton<ITournamentPlayerRepository, TournamentPlayerRepository>();
        services.AddSingleton<ITournamentLeaderboardRepository, TournamentLeaderboardRepository>();
        
        //Rest xd
        services.AddSingleton<ObsController>();
        
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ISettings>(s => s.GetRequiredService<SettingsService>());
        services.AddSingleton<ISettingsSaver>(s => s.GetRequiredService<SettingsService>());
        
        services.AddSingleton<IPresetSaver, PresetService>();
        services.AddSingleton<ITwitchService, TwitchService>();
        
        services.AddSingleton<ILeaderboardManager, LeaderboardManager>();
        services.AddSingleton<ILuaScriptsManager, LuaScriptsManager>();

        services.AddSingleton<StatusBarViewModel>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ICoordinator, MainCoordinator>();
        services.AddSingleton<IBackgroundCoordinator, BackgroundCoordinator>();
        
        services.AddSingleton<ControllerViewModel>();
        services.AddSingleton<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>();
        services.AddTransient<LeaderboardPanelViewModel>();
        services.AddTransient<SceneManagementViewModel>();
        services.AddTransient<UpdatesViewModel>();
        services.AddTransient<SettingsViewModel>();
        
        
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));

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
        
        var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
        LogService.Initialize(loggingService);
        
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var settingsService = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settingsService.Load();

        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        var startupSelectable = _serviceProvider.GetRequiredService<PresetManagerViewModel>();
        navigationService.Startup(startupSelectable);
        
        IInputController inputController = _serviceProvider.GetRequiredService<IInputController>();
        inputController.HotkeyPressed += HandleGeneralHotkeys;
        
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        IInputController inputController = _serviceProvider.GetRequiredService<IInputController>();
        inputController.HotkeyPressed -= HandleGeneralHotkeys;
        
        var settings = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settings.Save();
        
        _serviceProvider.Dispose();
        base.OnExit(e);
    }

    private void HandleGeneralHotkeys(HotkeyActionType actionType)
    {
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        
        switch (actionType)
        {
            case HotkeyActionType.General_SavePreset: 
                var presetSaver = _serviceProvider.GetRequiredService<IPresetSaver>();
                presetSaver.SavePreset();
                break;
            case HotkeyActionType.General_RenameElementOnMousePosition: 
                var textBlock = UIHelper.GetFocusedUIElement<EditableTextBlock>();
                if (textBlock is { IsEditable: true })
                {
                    textBlock.IsInEditMode = true;
                }
                break;
            case HotkeyActionType.General_ToggleDebugWindow:
                mainViewModel.SwitchDebugWindow();
                break;
            case HotkeyActionType.General_ToggleHamburgerMenu: 
                mainViewModel.IsHamburgerMenuOpen = !mainViewModel.IsHamburgerMenuOpen;
                break;
        }
    }
}