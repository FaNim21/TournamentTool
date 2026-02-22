using NSubstitute;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Entities.Ranking;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Background;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Lua;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Tests.Managers;

public class LeaderboardManagerTests
{
    private readonly ITournamentState _state = Substitute.For<ITournamentState>();
    private readonly ITournamentLeaderboardRepository _repo = Substitute.For<ITournamentLeaderboardRepository>();
    private readonly ILuaScriptsManager _lua = Substitute.For<ILuaScriptsManager>();
    private readonly ILoggingService _logger = Substitute.For<ILoggingService>();

    private LeaderboardManager CreateSut()
        => new LeaderboardManager(_state, _repo, _lua, _logger);

    private static Player CreatePlayer(int id)
        => new Player { UUID = Guid.NewGuid().ToString(), InGameName = $"Player{id}" };

    private static LeaderboardRankedEvaluateData CreateEval(int playerId, RunMilestone milestone, int time)
    {
        return new LeaderboardRankedEvaluateData(
            CreatePlayer(playerId),
            1,
            new LeaderboardTimeline(milestone, time),
            null);
    }
    
    [Fact]
    public void EvaluateTimelines_MaxWinners8_ShouldRemoveExactly8()
    {
        // Arrange
        const RunMilestone milestone = RunMilestone.ProjectEloComplete;

        var timeline = new RankedEvaluateTimelineData();
        for (int i = 0; i < 10; i++)
        {
            timeline.Evaluations.Add(
                new LeaderboardRankedEvaluateData(
                    new Player { UUID = Guid.NewGuid().ToString(), InGameName = $"P{i}" },
                    1,
                    new LeaderboardTimeline(milestone, 1000),
                    null));
        }

        var data = new Dictionary<RunMilestone, RankedEvaluateTimelineData>
        {
            { milestone, timeline }
        };

        var subRule = new LeaderboardSubRule
        {
            Time = 2000,
            MaxWinners = 8,
            LuaPath = "script.lua",
            Description = "Top 8"
        };

        var rule = new LeaderboardRule
        {
            IsEnabled = true,
            RuleType = LeaderboardRuleType.Split,
            ChosenAdvancement = milestone,
            SubRules = [subRule]
        };
        
        var state = Substitute.For<ITournamentState>();
        var logger = Substitute.For<ILoggingService>();
        
        var repo = Substitute.For<ITournamentLeaderboardRepository>();
        repo.Rules.Returns(new List<LeaderboardRule> { rule });
        repo.GetOrCreateEntry(Arg.Any<string>())
            .Returns(new LeaderboardEntry());

        var lua = Substitute.For<ILuaScriptsManager>();
        var sut = new LeaderboardManager(state, repo, lua, logger);

        // Act
        sut.EvaluateData(data);

        // Assert
        Assert.Equal(2, timeline.Evaluations.Count); // 10 - 8
    }
    
    [Fact]
    public void EvaluateTimelines_ExactlyMaxWinners_ShouldRemoveAll()
    {
        const RunMilestone milestone = RunMilestone.ProjectEloComplete;

        var timeline = new RankedEvaluateTimelineData();
        for (int i = 0; i < 8; i++)
        {
            timeline.Evaluations.Add(
                new LeaderboardRankedEvaluateData(
                    new Player { UUID = Guid.NewGuid().ToString(), InGameName = $"P{i}" },
                    1,
                    new LeaderboardTimeline(milestone, 1000),
                    null));
        }

        var data = new Dictionary<RunMilestone, RankedEvaluateTimelineData>
        {
            { milestone, timeline }
        };

        var subRule = new LeaderboardSubRule
        {
            Time = 2000,
            MaxWinners = 8,
            LuaPath = "script.lua"
        };

        var rule = new LeaderboardRule
        {
            IsEnabled = true,
            ChosenAdvancement = milestone,
            SubRules = [subRule]
        };

        var repo = Substitute.For<ITournamentLeaderboardRepository>();
        repo.Rules.Returns(new List<LeaderboardRule> { rule });
        repo.GetOrCreateEntry(Arg.Any<string>())
            .Returns(new LeaderboardEntry());

        var lua = Substitute.For<ILuaScriptsManager>();
        var sut = new LeaderboardManager(Substitute.For<ITournamentState>(), repo, lua, Substitute.For<ILoggingService>());

        sut.EvaluateData(data);

        Assert.Empty(timeline.Evaluations);
    }
    [Fact]
    public void EvaluateTimelines_MaxWinnersMinusOne_ShouldBeUnlimited()
    {
        const RunMilestone milestone = RunMilestone.ProjectEloComplete;

        var timeline = new RankedEvaluateTimelineData();
        for (int i = 0; i < 5; i++)
        {
            timeline.Evaluations.Add(
                new LeaderboardRankedEvaluateData(
                    new Player { UUID = Guid.NewGuid().ToString(), InGameName = $"P{i}" },
                    1,
                    new LeaderboardTimeline(milestone, 1000),
                    null));
        }

        var data = new Dictionary<RunMilestone, RankedEvaluateTimelineData>
        {
            { milestone, timeline }
        };

        var subRule = new LeaderboardSubRule
        {
            Time = 2000,
            MaxWinners = -1,
            LuaPath = "script.lua"
        };

        var rule = new LeaderboardRule
        {
            IsEnabled = true,
            ChosenAdvancement = milestone,
            SubRules = [subRule]
        };

        var repo = Substitute.For<ITournamentLeaderboardRepository>();
        repo.Rules.Returns(new List<LeaderboardRule> { rule });
        repo.GetOrCreateEntry(Arg.Any<string>())
            .Returns(new LeaderboardEntry());

        var lua = Substitute.For<ILuaScriptsManager>();
        var sut = new LeaderboardManager(Substitute.For<ITournamentState>(), repo, lua, Substitute.For<ILoggingService>());

        sut.EvaluateData(data);

        Assert.Empty(timeline.Evaluations);
    }
}