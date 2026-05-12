using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TournamentTool.App.Extensions;
using TournamentTool.App.Services;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    
    
    public App()
    {
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        IServiceCollection services = new ServiceCollection();
        
        services.AddDI();

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