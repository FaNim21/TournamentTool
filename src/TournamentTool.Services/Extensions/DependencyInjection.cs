using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ObsWebSocket.Core.Serialization;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Background;
using TournamentTool.Services.Configuration;
using TournamentTool.Services.External;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;
using TournamentTool.Services.Obs;
using TournamentTool.Services.Obs.Binding;

namespace TournamentTool.Services.Extensions;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddOBS();
        services.AddSettings();
        services.AddHttpAPIs();
        services.AddPreset();
        
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IBackgroundCoordinator, BackgroundCoordinator>();
        services.AddSingleton<IPopupNotificationService, PopupNotificationService>();
        
        services.AddSingleton<IPresetSaver, PresetService>();
        services.AddSingleton<ITwitchService, TwitchService>();
        
        services.AddSingleton<ILeaderboardManager, LeaderboardManager>();
        services.AddSingleton<ILuaScriptsManager, LuaScriptsManager>();
        
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<ILogStore, LogStore>();
    }
    
    private static void AddOBS(this IServiceCollection services)
    {
        services.AddSingleton<IObsController, ObsController>();
        services.AddSingleton<IObsUpdateBatcher, ObsUpdateBatcher>();
        
        services.AddSingleton<IBindingSchemaInitializer, BindingSchemaInitializer>();
        services.AddSingleton<IBindingEngine, BindingEngine>();
        
        //zrobic jako zwykly add singleton dla web socketu
        services.TryAddSingleton<JsonMessageSerializer>();
        services.AddSingleton<IWebSocketMessageSerializer>(sp => sp.GetRequiredService<JsonMessageSerializer>());
    }
    
    private static void AddSettings(this IServiceCollection services)
    {
        services.AddSingleton<SettingsProvider>();
        services.AddSingleton<ISettingsProvider>(s => s.GetRequiredService<SettingsProvider>());
        services.AddSingleton<ISettingsSaver>(s => s.GetRequiredService<SettingsProvider>());
    }
    
    private static void AddHttpAPIs(this IServiceCollection services)
    {
        services.AddSingleton<IMinecraftDataService, MinecraftDataService>();
        services.AddSingleton<IPacemanAPIService, PacemanAPIService>();
        services.AddSingleton<IRankedAPIService, RankedAPIService>();
        services.AddSingleton<IUpdateCheckerService, UpdateCheckerService>();
    }
    
    private static void AddPreset(this IServiceCollection services)
    {
        services.AddSingleton<ITournamentState, TournamentState>();
        services.AddSingleton<ITournamentPresetManager, TournamentPresetManager>();
        services.AddSingleton<ITournamentPlayerRepository, TournamentPlayerRepository>();
        services.AddSingleton<ITournamentLeaderboardRepository, TournamentLeaderboardRepository>();
    }
}