using Microsoft.Extensions.DependencyInjection;
using TournamentTool.Presentation.Factories;
using TournamentTool.Presentation.Obs;
using TournamentTool.Services.Obs;

namespace TournamentTool.Presentation.Extensions;

public static class DependencyInjection
{
    public static void AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<SceneManager>();
        services.AddSingleton<ISceneManager>(sp => sp.GetRequiredService<SceneManager>());
        services.AddSingleton<ISceneItemGetter>(sp => sp.GetRequiredService<SceneManager>());
        
        services.AddSingleton<ISceneFactory, SceneFactory>();
    }
}