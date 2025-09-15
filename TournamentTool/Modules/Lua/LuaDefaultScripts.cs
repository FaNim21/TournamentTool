using NuGet.Versioning;

namespace TournamentTool.Modules.Lua;

public static class LuaDefaultScripts
{
    public record DefaultScript(NuGetVersion Version, string Code);

    public static readonly Dictionary<string, DefaultScript> DefaultScripts = new()
    {
        ["normal_add_base_points"] = new DefaultScript(NuGetVersion.Parse("1.0.0"),
            """
            version = "1.0.0"
            type = "normal"
            description = "Basic script adding base point to successfully evaluated player.\ncount - displays amount of evaluated milestones\nlast_player - displays last evaluated player ign"
            
            register_variable("count", "number", 0)
            register_variable("last_player", "string", "none")
            
            function evaluate_data(api)
                api:set_variable("count", api:get_variable("count") + 1)
                api:set_variable("last_player", api.player_name)
            
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
