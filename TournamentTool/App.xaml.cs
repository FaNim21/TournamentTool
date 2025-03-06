using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TournamentTool.Interfaces;
using TournamentTool.Modules;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Utils;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;


    public App()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });

        services.AddSingleton<TournamentViewModel>();
        services.AddSingleton<IPresetSaver, PresetService>();
        
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ICoordinator, MainViewModelCoordinator>();
        
        services.AddSingleton<ControllerViewModel>();
        services.AddSingleton<PresetManagerViewModel>();
        services.AddSingleton<PlayerManagerViewModel>();
        
        services.AddSingleton<LeaderboardPanelViewModel>();
        services.AddSingleton<SceneManagementViewModel>();

        services.AddSingleton<UpdatesViewModel>();
        services.AddSingleton<SettingsViewModel>();

        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<Func<Type, SelectableViewModel>>(serviceProvider => viewModelType => (SelectableViewModel)serviceProvider.GetRequiredService(viewModelType));

        _serviceProvider = services.BuildServiceProvider();
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
        base.OnExit(e);
    }
}
