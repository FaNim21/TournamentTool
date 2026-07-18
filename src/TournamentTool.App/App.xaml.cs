using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TournamentTool.App.Extensions;
using TournamentTool.App.Services;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Selectable;

namespace TournamentTool.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;
    private bool _handledCrash;
    
    
    public App()
    {
        System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        IServiceCollection services = new ServiceCollection();
        
        services.AddDI();

        _serviceProvider = services.BuildServiceProvider();
        
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
        };

        Current.DispatcherUnhandledException += (_, e) =>
        {
            LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
            // e.Handled = true; //Problem z tym, ze to blokuje wywalanie aplikacji, a nie wiem czy to sie do czegos ma jak i tak nie ma UI xd
        };

        Dispatcher.UnhandledException += (_, e) =>
        {
            LogUnhandledException(e.Exception, "Dispatcher.UnhandledException");
            // e.Handled = true;
        };

        /*TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            // LogUnhandledExceptionAsNotCrashed(e.Exception, "TaskScheduler.UnobservedTaskException");
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };*/
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Directory.CreateDirectory(Consts.PresetsPath);
        Directory.CreateDirectory(Consts.LogsPath);
        Directory.CreateDirectory(Consts.ScriptsPath);
        Directory.CreateDirectory(Consts.LeaderboardScriptsPath);
        
        MainWindow mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.DisableStylusAndTouchSupport", true);
        AppContext.SetSwitch("Switch.System.Windows.Input.Stylus.EnablePointerSupport", true);
        
        ISettingsSaver settingsSaver = _serviceProvider.GetRequiredService<ISettingsSaver>();
        settingsSaver.Load();
        
        ILoggingService loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
        LogHelper.Initialize(loggingService);
        
        IApplicationLifetime applicationLifetime = _serviceProvider.GetRequiredService<IApplicationLifetime>();
        applicationLifetime.OnStartup();
        
        INavigationService navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        PresetManagerViewModel startupSelectable = _serviceProvider.GetRequiredService<PresetManagerViewModel>();
        navigationService.Startup(startupSelectable);

        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
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
    
    private void LogUnhandledException(Exception exception, string source)
    {
        if (_handledCrash) return;
        _handledCrash = true;
        
        try
        {
            ISettingsSaver settingsSaver = _serviceProvider.GetRequiredService<ISettingsSaver>();
            settingsSaver.Save();
            
            IPresetSaver presetSaver = _serviceProvider.GetRequiredService<IPresetSaver>();
            presetSaver.SavePreset();
        }
        catch
        {
            // ignored
        }

        try
        {
            IDialogService dialogService = _serviceProvider.GetRequiredService<IDialogService>();
            dialogService.Show($"Unhandled exception\n(Check %Appdata%\\TournamentTool\\Logs for more info): {exception.Message}", "Application crash", Domain.Enums.MessageBoxButton.OK, Domain.Enums.MessageBoxImage.Error);
        }
        catch
        {
            // ignored
        }

        try
        {
            ILogStore logStore = _serviceProvider.GetRequiredService<ILogStore>();
            Task.Run(async () => await logStore.SaveToFileAsync());
        }
        catch
        {
            // ignored
        }

        string output = $"UnhandledException ({source}): {exception}";
        Helper.SaveLog(output, "crash_log");
    }

}