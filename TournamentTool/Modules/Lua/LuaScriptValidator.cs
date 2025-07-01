using System.IO;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.Lua;

public static class LuaScriptValidator
{
    public static LuaScriptValidationResult ValidateScriptWithRuntime(string scriptPath, LuaLeaderboardType type)
    {
        try
        {
            var code = File.ReadAllText(scriptPath);
            var script = new LuaLeaderboardScript(code, scriptPath);
            var result = new LuaScriptValidationResult();

            // Syntax
            var syntaxResult = script.ValidateSyntax();
            if (!syntaxResult.IsValid)
            {
                result.SyntaxError = syntaxResult.SyntaxError;
                result.IsValid = false;
                return result;
            }

            script.ExtractMetadata();

            // Runtime
            var testContext = CreateTestContext(script.Type);
            if (testContext != null)
            {
                var runtimeResult = script.ValidateRuntime(testContext);
                result.RuntimeErrors.AddRange(runtimeResult.RuntimeErrors);
                result.Warnings.AddRange(runtimeResult.Warnings);
            }
        
            result.IsValid = !result.HasErrors;
            script.SetValidation(result.IsValid);
        
            return result;
        }
        catch (Exception ex)
        {
            return new LuaScriptValidationResult
            {
                IsValid = false,
                SyntaxError = new LuaScriptError
                {
                    Type = "Error",
                    Message = ex.Message
                }
            };
        }
    }
    private static object CreateTestContext(LuaLeaderboardType type)
    {
        if (type == LuaLeaderboardType.ranked)
        {
            var players = new List<LuaPlayerData>
            {
                CreateMockPlayer("TestPlayer1", 1, 100, 60000),
                CreateMockPlayer("TestPlayer2", 2, 80, 70000)
            };

            LeaderboardSubRule subRule = new() {BasePoints = 100};
            TournamentViewModel tournament = new TournamentViewModel();
            tournament.ManagementData = new RankedManagementData { Rounds = 1, Completions = 1, Players = 2 };
            
            LuaAPIRankedContext context = new LuaAPIRankedContext(subRule, tournament, players, null);
            return context;
        }
        else
        {
            LeaderboardSubRule subRule = new() {BasePoints = 100};
            LeaderboardEntry entry = new LeaderboardEntry { Points = 5, Position = 2 };
            
            LeaderboardTimeline mainSplit = new LeaderboardTimeline(RunMilestone.PacemanEnterNether, 12345);
            LeaderboardPlayerEvaluateData data = new LeaderboardPacemanEvaluateData(new Player(), "asdf", mainSplit, null);
            
            TournamentViewModel tournament = new TournamentViewModel();
            tournament.ManagementData = new RankedManagementData { Rounds = 1, Completions = 1, Players = 2 };
            
            LuaAPIContext context = new LuaAPIContext(entry, data, subRule, tournament, null);
            return context;
        }
    }
    private static LuaPlayerData CreateMockPlayer(string name, int position, int points, int time)
    {
        var player = new Player { InGameName = name, UUID = Guid.NewGuid().ToString() };
        var timeline = new LeaderboardTimeline(RunMilestone.StoryEnterTheEnd, time);
        var data = new LeaderboardPacemanEvaluateData(player, "test", timeline, null);
        var entry = new LeaderboardEntry 
        { 
            Position = position, 
            Points = points, 
            PlayerUUID = player.UUID
        };
        
        return new LuaPlayerData(entry, data);
    }
}
