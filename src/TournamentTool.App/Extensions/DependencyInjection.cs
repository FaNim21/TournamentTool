using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using TournamentTool.App.Services;
using TournamentTool.Core.Interfaces;
using TournamentTool.Core.Utils;
using TournamentTool.Presentation.Extensions;
using TournamentTool.Services.Extensions;
using TournamentTool.Services.State;
using TournamentTool.ViewModels;
using TournamentTool.ViewModels.Extensions;
using TournamentTool.ViewModels.Menu;

namespace TournamentTool.App.Extensions;

public static class DependencyInjection
{
    public static void AddDI(this IServiceCollection services)
    {
        services.AddConfiguration();
        services.AddApplication();
        services.AddPresentation();
        services.AddServices();
        services.AddViewModels();
    }
    
    private static void AddConfiguration(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.Configure<HttpClientFactoryOptions>(options =>
        {
            options.HttpClientActions.Add(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", $"TournamentTool/{Consts.Version}");
                client.Timeout = TimeSpan.FromSeconds(10);  //globalny timeout
            });
        }); 
        
        services.AddSingleton<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });
    }
    
    private static void AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IApplicationState, ApplicationState>();
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IInputController, InputController>();
        services.AddSingleton<IDataProtect, WindowsDataProtect>();
        services.AddSingleton<IMenuService, MenuService>();
        services.AddSingleton<IUIInteractionService, UIInteractionService>();
        
        services.AddSingleton<IApplicationLifetime, ApplicationLifetime>();
        services.AddSingleton<IImageService, ImageService>();
    }
}