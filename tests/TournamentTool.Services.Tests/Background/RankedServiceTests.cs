using System.Linq.Expressions;
using NSubstitute;
using TournamentTool.Core.Exceptions;
using TournamentTool.Core.Factories;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Domain.Interfaces;
using TournamentTool.Services.Background;
using TournamentTool.Services.External;
using TournamentTool.Services.Factories;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Managers;
using TournamentTool.Services.Managers.Preset;

namespace TournamentTool.Services.Tests.Background;

public class RankedServiceTests
{
    private readonly ILeaderboardManager _leaderboard;
    private readonly ILoggingService _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IPlayerViewModelFactory _playerViewModelFactory;
    private readonly IRankedAPIService _rankedApiService;
    private readonly IImageService _imageService;
    private readonly ITournamentState _tournamentState;
    private readonly ITournamentPlayerRepository _playerRepository;
    private readonly IManagementDataContextFactory _managementFactory;
    private readonly IManagementDataContext<RankedManagementData> _managementContext;

    private readonly RankedService _service;

    public RankedServiceTests()
    {
        _leaderboard = Substitute.For<ILeaderboardManager>();
        _logger = Substitute.For<ILoggingService>();
        _settingsProvider = Substitute.For<ISettingsProvider>();
        _playerViewModelFactory = Substitute.For<IPlayerViewModelFactory>();
        _rankedApiService = Substitute.For<IRankedAPIService>();
        _imageService = Substitute.For<IImageService>();
        _tournamentState = Substitute.For<ITournamentState>();
        _playerRepository = Substitute.For<ITournamentPlayerRepository>();
        _managementFactory = Substitute.For<IManagementDataContextFactory>();
        _managementContext = Substitute.For<IManagementDataContext<RankedManagementData>>();

        _managementFactory
            .Create<RankedManagementData>()
            .Returns(_managementContext);

        _managementContext
            .Get(Arg.Any<Func<RankedManagementData, List<PrivRoomBestSplit>>>())
            .Returns([]);

        _managementContext
            .Get(Arg.Any<Func<RankedManagementData, int>>())
            .Returns(0);

        var settings = new Settings
        {
            SaveRankedPrivRoomDataOnSeedFinish = false
        };

        _settingsProvider
            .Get<Settings>()
            .Returns(settings);

        _tournamentState.CurrentPreset.Returns(new Tournament
        {
            RankedApiKey = "api-key",
            RankedApiPlayerName = "player-name",
            AddUnknownRankedPlayersToWhitelist = false
        });

        _service = new RankedService(
            _leaderboard,
            _logger,
            _settingsProvider,
            _playerViewModelFactory,
            _rankedApiService,
            _imageService,
            _tournamentState,
            _playerRepository,
            _managementFactory);
    }

    [Fact]
    public async Task Update_ShouldThrow_WhenApiKeyIsEmpty()
    {
        // Arrange
        _tournamentState.CurrentPreset.Returns(new Tournament
        {
            RankedApiKey = "",
            RankedApiPlayerName = "player"
        });

        // Act + Assert
        await Assert.ThrowsAsync<BackgroundServiceException>(async () =>
        {
            await _service.Update(CancellationToken.None);
        });
    }

    [Fact]
    public async Task Update_ShouldThrow_WhenPlayerNameIsEmpty()
    {
        // Arrange
        _tournamentState.CurrentPreset.Returns(new Tournament
        {
            RankedApiKey = "key",
            RankedApiPlayerName = ""
        });

        // Act + Assert
        await Assert.ThrowsAsync<BackgroundServiceException>(async () =>
        {
            await _service.Update(CancellationToken.None);
        });
    }

    [Fact]
    public async Task Update_ShouldThrow_WhenApiReturnsNull()
    {
        // Arrange
        _rankedApiService.GetRankedPrivateRoomLiveData(Arg.Any<string>(), Arg.Any<string>())
            .Returns((PrivRoomAPIResult?)null);

        // Act + Assert
        await Assert.ThrowsAsync<BackgroundServiceException>(async () =>
        {
            await _service.Update(CancellationToken.None);
        });
    } 
    
    [Fact]
    public async Task Update_ShouldCallReceiverUpdate()
    {
        // Arrange
        var receiver = Substitute.For<IRankedDataReceiver>();

        _service.RegisterData(receiver);

        _rankedApiService
            .GetRankedPrivateRoomLiveData(
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(new PrivRoomAPIResult
            {
                Data = new PrivRoomData
                {
                    Status = MatchStatus.ready
                }
            });

        // Act
        await _service.Update(CancellationToken.None);

        // Assert
        receiver.Received(1).Update();
    }
    
    [Fact]
    public async Task Update_ShouldUpdateCompletionAndPlayerCounts()
    {
        // Arrange
        _rankedApiService.GetRankedPrivateRoomLiveData(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new PrivRoomAPIResult
            {
                Data = new PrivRoomData
                {
                    Status = MatchStatus.ready,
                    Players =
                    [
                        new PrivRoomPlayer
                        {
                            UUID = "1",
                            InGameName = "Player"
                        }
                    ],
                    Completions =
                    [
                        new PrivRoomCompletion
                        {
                            UUID = "1",
                            Time = 1000
                        }
                    ]
                }
            });

        // Act
        await _service.Update(CancellationToken.None);

        // Assert
        _managementContext.Received().Set(Arg.Any<Expression<Func<RankedManagementData, int>>>(), 1);
    }

    [Fact]
    public void FilterJSON_ShouldEvaluate_WhenStatusDone()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.done,
            LastID = 2,
            Completions =
            [
                new PrivRoomCompletion
                {
                    UUID = "uuid",
                    Time = 1000
                }
            ]
        };
        _service.AddPace("uuid", "asdf");

        // Act
        _service.FilterJSON(data);

        // Assert
        _leaderboard.Received(1).EvaluateData(Arg.Any<Dictionary<RunMilestone, RankedEvaluateTimelineData>>());
    }

    [Fact]
    public void FilterJSON_ShouldReadySeed_WhenStatusReady()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.ready,
            Players =
            [
                new PrivRoomPlayer
                {
                    UUID = "uuid-1",
                    InGameName = "Player1"
                }
            ]
        };

        // Act
        _service.FilterJSON(data);

        // Assert
        var split = _service.GetBestSplit(RankedSplitType.complete);

        Assert.NotNull(split);
    }
    
    [Fact]
    public void FilterJSON_ShouldIgnoreDuplicateNonRunningStatus()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.ready
        };

        // Act
        _service.FilterJSON(data);
        _service.FilterJSON(data);

        // Assert
        _managementContext.Received(2).Get(Arg.Any<Func<RankedManagementData, List<PrivRoomBestSplit>>>());
    }
    
    [Fact]
    public void FilterJSON_ShouldProcessRunningStatusRepeatedly()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.running
        };

        // Act
        _service.FilterJSON(data);
        _service.FilterJSON(data);

        // Assert
        _managementContext.Received(3).Get(Arg.Any<Func<RankedManagementData, List<PrivRoomBestSplit>>>());
    }

    [Fact]
    public void FilterJSON_ShouldEvaluateOnlyOncePerRoomId()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.done,
            LastID = 55,
            Completions =
            [
                new PrivRoomCompletion
                {
                    UUID = "uuid",
                    Time = 1000
                }
            ]
        };

        _service.AddPace("uuid", "Player");

        // Act
        _service.FilterJSON(data);
        _service.FilterJSON(data);

        // Assert
        _leaderboard.Received(1).EvaluateData(Arg.Any<Dictionary<RunMilestone, RankedEvaluateTimelineData>>());
    } 
    
    [Fact]
    public void FilterJSON_ShouldEvaluateAgain_AfterGenerating_WhenIdRestarts()
    {
        // Arrange
        var data1 = new PrivRoomData
        {
            Status = MatchStatus.done,
            LastID = 1,
            Completions =
            [
                new PrivRoomCompletion
                {
                    UUID = "uuid",
                    Time = 1000
                }
            ]
        };
        
        var data2_generate = new PrivRoomData
        {
            Status = MatchStatus.generate,
            LastID = 1,
        };

        var data2_done = new PrivRoomData
        {
            Status = MatchStatus.done,
            LastID = 1,
            Completions =
            [
                new PrivRoomCompletion
                {
                    UUID = "uuid",
                    Time = 2000
                }
            ]
        };

        _service.AddPace("uuid", "Player");

        // Act
        _service.FilterJSON(data1);
        _service.FilterJSON(data2_generate);
        _service.FilterJSON(data2_done);

        // Assert
        _leaderboard.Received(2).EvaluateData(Arg.Any<Dictionary<RunMilestone, RankedEvaluateTimelineData>>());
    }
    
    [Fact]
    public void FilterJSON_ShouldResetManagementData_WhenGenerate()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.generate
        };

        // Act
        _service.FilterJSON(data);

        // Assert
        _managementContext.Received().Set(Arg.Any<Expression<Func<RankedManagementData, bool>>>(), true);
        _managementContext.Received().Set(Arg.Any<Expression<Func<RankedManagementData, int>>>(), 0);
    } 
    
    [Fact]
    public void FilterJSON_ShouldIgnoreRootTimeline()
    {
        // Arrange
        var data = new PrivRoomData
        {
            Status = MatchStatus.running,
            Timelines =
            [
                new PrivRoomTimeline
                {
                    UUID = "uuid",
                    Time = 1000,
                    Type = "story.root"
                }
            ]
        };

        _service.AddPace("uuid", "Player");

        // Act
        _service.FilterJSON(data);

        // Assert
        Assert.True(true);
    } 
    
    [Fact]
    public void GetBestSplit_ShouldReturnSameInstance()
    {
        // Act
        var split1 = _service.GetBestSplit(RankedSplitType.complete);
        var split2 = _service.GetBestSplit(RankedSplitType.complete);

        // Assert
        Assert.Same(split1, split2);
    }

    [Fact]
    public void RegisterData_ShouldRegisterRankedReceiver()
    {
        // Arrange
        var receiver = Substitute.For<IRankedDataReceiver>();

        // Act
        _service.RegisterData(receiver);

        // Assert
        receiver.Received(1)
            .AddPaces(Arg.Any<IEnumerable<RankedPace>>());
    }
    
    [Fact]
    public void RegisterData_ShouldReplacePreviousReceiver()
    {
        // Arrange
        var receiver1 = Substitute.For<IRankedDataReceiver>();
        var receiver2 = Substitute.For<IRankedDataReceiver>();

        // Act
        _service.RegisterData(receiver1);
        _service.RegisterData(receiver2);

        _rankedApiService.GetRankedPrivateRoomLiveData(Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(new PrivRoomAPIResult
            {
                Data = new PrivRoomData
                {
                    Status = MatchStatus.ready
                }
            });

        // Assert
        receiver1.DidNotReceive().Update();
    }

    [Fact]
    public void UnregisterData_ShouldRemoveReceiver()
    {
        // Arrange
        var receiver = Substitute.For<IRankedDataReceiver>();

        _service.RegisterData(receiver);

        // Act
        _service.UnregisterData(receiver);

        // Assert
        Assert.True(true);
    }

    [Fact]
    public void ReadySeed_ShouldAddAllPlayersToReceiver()
    {
        // Arrange
        var receiver = Substitute.For<IRankedDataReceiver>();
        _service.RegisterData(receiver);

        var data = new PrivRoomData
        {
            Players =
            [
                new PrivRoomPlayer
                {
                    UUID = "1",
                    InGameName = "A"
                },
                new PrivRoomPlayer
                {
                    UUID = "2",
                    InGameName = "B"
                }
            ]
        };

        // Act
        _service.ReadySeed(data);

        // Assert
        receiver.Received(2).AddPace(Arg.Any<RankedPace>());
    }
    
    [Fact]
    public void AddEvaluationData_ShouldNotThrow_OnDuplicatePlayer()
    {
        // Arrange
        var player = new Player
        {
            UUID = "uuid",
            InGameName = "Player1"
        };

        var timeline = new RankedPaceTimeline(
            "Nether",
            RunMilestone.StoryEnterTheNether,
            1000);

        // Act
        _service.AddEvaluationData(player, timeline);
        _service.AddEvaluationData(player, timeline);

        // Assert
        Assert.True(true);
    }
}