using NuGet.Versioning;

namespace TournamentTool.Modules.Lua;

public static class LuaDefaultScripts
{
    public record DefaultScript(NuGetVersion Version, string Code);

    public static readonly Dictionary<string, DefaultScript> DefaultScripts = new()
    {
        ["normal_add_base_points"] = new DefaultScript(NuGetVersion.Parse("0.2.0"),
            """
            version = "0.2.0"
            type = "normal"
            description = "Basic script adding base point to successfully evaluated player"

            function evaluate_data(api)
                api:register_milestone(api.base_points)
            end
            """),
        ["ranked_add_base_points"] = new DefaultScript(NuGetVersion.Parse("0.2.0"),
            """
            version = "0.2.0"
            type = "ranked"
            description = "Basic Ranked type script adding base point to all evaluated players"

            function evaluate_data(api)
                for _, player in ipairs(api.players) do
                    api:register_milestone(player, api.base_points)
                end
            end
            """),
    };

}
