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
        ["ranked_add_base_points"] = new DefaultScript(NuGetVersion.Parse("1.0.0"),
            """
            version = "1.0.0"
            type = "ranked"
            description = "Basic Ranked type script adding base point to all evaluated players.\namount_players_evaluated - displays amount of players evaluated last round"
            
            register_variable("amount_players_evaluated", "number", 0)
            
            function evaluate_data(api)
                api:set_variable("amount_players_evaluated", #api.players)
            
                for _, player in ipairs(api.players) do
                    api:register_milestone(player, api.base_points)
                end
            end
            """),
        ["dynamic_points_logic"] = new DefaultScript(NuGetVersion.Parse("2.0.0"),
            """
            version = "2.0.0"
            type = "ranked"
            description = "Points evaluation based on showdown/lcq points distribution.\nplayers_amount - last number of players evaluated by the script"
            
            register_variable("players_amount", "number", 0)
            
            function evaluate_data(api)
                api:set_variable("playersAmount", #api.players)
            
            	print("Evaluation for: ", api.milestone_name, ", in round: ", api.round)
            	print("==================================================================")
            	for i, player in ipairs(api.players) do
            		local points = round((#api.players - (i - 1)) / #api.players * api.base_points)
            		print(i - 1, " - player: ", player.name, ", points: ", points)
            
            		api:register_milestone(player, points)
            	end
            	print("==================================================================")
            end
            
            function round(x)
            	return math.floor(x + 0.5)
            end
            """),
    };

}
