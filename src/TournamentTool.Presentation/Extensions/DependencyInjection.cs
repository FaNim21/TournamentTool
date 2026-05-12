using Microsoft.Extensions.DependencyInjection;
using TournamentTool.Presentation.Obs;

namespace TournamentTool.Presentation.Extensions;

public static class DependencyInjection
{
    public static void AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<ISceneManager, SceneManager>();
    }
}