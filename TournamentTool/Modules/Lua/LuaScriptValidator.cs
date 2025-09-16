using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Models.Ranking;
using TournamentTool.ViewModels.Entities;

namespace TournamentTool.Modules.Lua;

public static class LuaScriptValidator
{
    public static object CreateTestContext(LuaLeaderboardScript script)
    {
        LeaderboardRule rule = new() { Name = "xd", ChosenAdvancement = RunMilestone.ProjectEloComplete, RuleType = LeaderboardRuleType.Split };
        LeaderboardSubRule subRule = new() { BasePoints = 63 };
        
        foreach (var variable in script.CustomVariables)
        {
            subRule.CustomVariables[variable.Name] = variable;
        }
        
        if (script.Type == LuaLeaderboardType.ranked)
        {
            var players = new List<LuaPlayerData>
            {
                CreateMockPlayer("TestPlayer1", 1, 100, 60000),
                CreateMockPlayer("TestPlayer2", 2, 80, 70000),
                CreateMockPlayer("TestPlayer3", 3, 73, 90000),
                CreateMockPlayer("TestPlayer4", 4, 47, 120500),
            };

            TournamentViewModel tournament = new TournamentViewModel();
            tournament.ManagementData = new RankedManagementData { Rounds = 7, Completions = 3, Players = 4 };
            
            LuaAPIRankedContext context = new LuaAPIRankedContext(rule, subRule, tournament, players, null);
            return context;
        }
        else
        {
            LeaderboardEntry entry = new LeaderboardEntry { Points = 5, Position = 2 };
            
            LeaderboardTimeline mainSplit = new LeaderboardTimeline(RunMilestone.PacemanEnterNether, 12345);
            LeaderboardPlayerEvaluateData data = new LeaderboardPacemanEvaluateData(new Player(), "asdf", mainSplit, null);
            
            TournamentViewModel tournament = new TournamentViewModel();
            tournament.ManagementData = new RankedManagementData { Rounds = 1, Completions = 1, Players = 2 };
            
            LuaAPIContext context = new LuaAPIContext(entry, data, rule, subRule, tournament, null);
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