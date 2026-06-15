using NSubstitute;
using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Presentation.Obs;
using TournamentTool.Presentation.Obs.Entities;
using TournamentTool.Services.Logging;
using TournamentTool.Services.Obs;

namespace TournamentTool.ViewModels.Tests;

public class PointOfViewViewModelTests
{
    private readonly ISceneManager _canvasViewModel = Substitute.For<ISceneManager>();
    private readonly ILoggingService _logger = Substitute.For<ILoggingService>();
    private readonly IScene _scene = Substitute.For<IScene>();
    
    private readonly PointOfView _sut;
    
    
    protected PointOfViewViewModelTests()
    {
        _sut = CreatePOV(1234, SceneType.Main, false);
    }

    public PointOfView CreatePOV(int id, SceneType type = SceneType.Main, bool useGroup = true)
    {
        var transform = new SceneItemTransformStub(
            100, 200, 0, 1, 1, 1920, 1080,
            null,null,null,null,null,null,
            null,null,null,null,null);

        SceneItemStub item = new( 1, 0, $"Item{id}", "", true, false, transform);
        SceneItemStub? group = new( 1, 0, $"Group{id}", "", true, false, transform, true);

        if (!useGroup)
        {
            group = null;
        }
        
        var pov = new PointOfView(_canvasViewModel, _logger, type);
        pov.Initialize(_scene, item, group);

        return pov;
    }
        
    public IPlayer CreatePlayer(string name = "Player", bool whitelist = true)
    {
        IPlayer? player = Substitute.For<IPlayer>();

        player.DisplayName.Returns(name);
        player.StreamDisplayInfo.Returns(new StreamDisplayInfo(name, StreamType.twitch));
        player.IsFromWhitelist.Returns(whitelist);

        player.IsUsedInPov = false;
        player.IsUsedInPreview = false;

        return player;
    }

    public class CoreFunctionalityViewModelTests : PointOfViewViewModelTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Clear_ResetsStateCorrectly(bool fullClear)
        {
            _sut.DisplayedPlayer = "Player";
            _sut.Volume = 50;
            _sut.CustomStreamName = "CustomStream";

            _sut.Clear(fullClear);

            Assert.Equal(string.Empty, _sut.DisplayedPlayer);
            Assert.Equal(0, _sut.Volume);
            Assert.Null(_sut.Player);
            Assert.Equal(fullClear ? string.Empty : "CustomStream", _sut.CustomStreamName);
        }
    }

    public class VolumeViewModelTests : PointOfViewViewModelTests
    {
        [Theory]
        [InlineData(50, 50, 50)] // Same volume - no change to NewVolume
        [InlineData(50, 75, 75)] // Different volume - updates NewVolume
        public void ChangeVolume_UpdatesCorrectly(int currentVolume, int newVolume, int expectedNewVolume)
        {
            _sut.Volume = currentVolume;
            _sut.NewVolume = currentVolume;

            _sut.ChangeVolume(newVolume);

            Assert.Equal(newVolume, _sut.Volume);
            Assert.Equal(expectedNewVolume, _sut.NewVolume);
        }
    }

    public class SetPovViewModelTests : PointOfViewViewModelTests
    {
        private IPlayer CreateMockPlayer(bool isFromWhitelist = true, string displayName = "TestPlayer")
        {
            var player = Substitute.For<IPlayer>();
            
            player.DisplayName.Returns(displayName);
            player.StreamDisplayInfo.Returns(new StreamDisplayInfo("TestStream", StreamType.twitch));
            player.HeadViewParameter.Returns("HeadParam");
            player.GetPersonalBest.Returns("1:00:00");
            player.IsFromWhitelist.Returns(isFromWhitelist);

            player.IsUsedInPov = false;
            player.IsUsedInPreview = false;

            return player;
        }

        [Fact]
        public void SetPOV_WithNull_ClearsEverything()
        {
            _sut.Player = CreateMockPlayer();
            
            _sut.SetPOV(null);
            
            Assert.True(_sut.IsEmpty);
        }

        [Fact]
        public void SetPOV_WithWhitelistedPlayer_SetsDirectly()
        {
            var player = CreateMockPlayer(isFromWhitelist: true);

            _sut.SetPOV(player);

            Assert.Equal("TestPlayer", _sut.DisplayedPlayer);
            Assert.False(_sut.IsEmpty);
        }

        [Fact]
        public void SetPOV_WithNonWhitelistedPlayer_CreatesCustomPOV()
        {
            var player = CreateMockPlayer(isFromWhitelist: false);

            _sut.SetPOV(player);

            Assert.Equal("TestStream", _sut.CustomStreamName);
            Assert.Equal("TestStream", _sut.DisplayedPlayer);
        }

        [Theory]
        [InlineData(SceneType.Main, true, false)]
        [InlineData(SceneType.Preview, false, true)]
        public void SetPOV_UpdatesCorrectUsageFlag(SceneType sceneType, bool expectedUsedInPov, bool expectedUsedInPreview)
        {
            var pov = CreatePOV(1, sceneType);
            IPlayer player = CreateMockPlayer();

            pov.SetPOV(player);

            Assert.Equal(expectedUsedInPov, player.IsUsedInPov);
            Assert.Equal(expectedUsedInPreview, player.IsUsedInPreview);
        }
    }

    public class SetCustomPovViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public void SetCustomPOV_WithEmptyName_ClearsIfPreviouslySet()
        {
            _sut.CustomStreamName = "PreviousStream";
            _sut.SetCustomPOV();

            _sut.CustomStreamName = "";
            _sut.SetCustomPOV();

            Assert.Equal(string.Empty, _sut.CurrentCustomStreamName);
        }

        [Theory]
        [InlineData("NewStream", StreamType.twitch)]
        [InlineData("YouTubeStream", StreamType.youtube)]
        public void SetCustomPOV_CreatesCustomPlayer(string streamName, StreamType streamType)
        {
            _sut.CustomStreamName = streamName;
            _sut.CustomStreamType = streamType;

            _sut.SetCustomPOV();

            Assert.Equal(streamName, _sut.DisplayedPlayer);
            Assert.Equal(streamName, _sut.CurrentCustomStreamName);
            Assert.Equal(streamType, _sut.CurrentCustomStreamType);
        }
    }

    public class SwapViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public void Swap_WithNull_ReturnsFalse()
        {
            bool result = _sut.Swap(null);

            Assert.False(result);
        }

        [Fact]
        public void Swap_WithDifferentSceneType_ReturnsFalse()
        {
            PointOfView otherPov = CreatePOV(2, SceneType.Preview);
            
            bool result = _sut.Swap(otherPov);

            Assert.False(result);
        }

        [Fact]
        public void Swap_WithSameSceneType_SwapsSuccessfully()
        {
            PointOfView otherPov = CreatePOV(2);
            
            _sut.CustomStreamName = "Stream1";
            otherPov.CustomStreamName = "Stream2";

            bool result = _sut.Swap(otherPov);

            Assert.True(result);
            Assert.Equal("Stream2", _sut.CustomStreamName);
            Assert.Equal("Stream1", otherPov.CustomStreamName);
        }
    }

    public class OnPovClickSimulationViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public void OnPOVClick_SwapBetweenPOVsWithPlayers()
        {
            // Simulate: Swap between two POVs with players
            PointOfView pov1 = CreatePOV(1);
            PointOfView pov2 = CreatePOV(2);
            IPlayer player1 = CreatePlayer("Player1");
            IPlayer player2 = CreatePlayer("Player2");
            
            pov1.SetPOV(player1);
            pov2.SetPOV(player2);
            
            bool swapResult = pov1.Swap(pov2);
            
            Assert.True(swapResult);
            Assert.Equal("Player2", pov1.DisplayedPlayer);
            Assert.Equal("Player1", pov2.DisplayedPlayer);
        }

        [Fact]
        public void OnPOVClick_SetPlayerToPOV()
        {
            // Simulate: Click on POV with a player selected
            PointOfView pov = CreatePOV(1);
            IPlayer player = CreatePlayer("TestPlayer");
            
            pov.SetPOV(player);
            
            Assert.Equal("TestPlayer", pov.DisplayedPlayer);
            Assert.True(player.IsUsedInPov);
        }

        [Theory]
        [InlineData(SceneType.Main, SceneType.Preview, false)] // Different scene types - no swap
        [InlineData(SceneType.Main, SceneType.Main, true)]     // Same scene type - swap allowed
        [InlineData(SceneType.Preview, SceneType.Preview, true)] // Same scene type - swap allowed
        public void OnPOVClick_BetweenDifferentSceneTypes(SceneType scene1Type, SceneType scene2Type, bool shouldSwap)
        {
            // Simulate: Interaction between POVs from different scenes
            PointOfView pov1 = CreatePOV(1, scene1Type);
            PointOfView pov2 = CreatePOV(2, scene2Type);
            IPlayer player = CreatePlayer("Player1");
            
            pov1.SetPOV(player);
            
            bool swapResult = pov1.Swap(pov2);
            
            Assert.Equal(shouldSwap, swapResult);
            if (shouldSwap)
            {
                Assert.Equal("Player1", pov2.DisplayedPlayer);
                Assert.False(pov2.IsEmpty);
            }
            else
            {
                Assert.True(pov2.IsEmpty);
            }
        }

        [Fact]
        public void OnPOVClick_ComplexScenario_MultipleClicks()
        {
            // Simulate complex scenario with multiple POVs and clicks
            PointOfView mainPov1 = CreatePOV(1);
            PointOfView mainPov2 = CreatePOV(2);
            PointOfView previewPov1 = CreatePOV(3, SceneType.Preview);
            IPlayer player1 = CreatePlayer("Player1");
            IPlayer player2 = CreatePlayer("Player2");
            
            // Step 1: Set player1 to mainPov1
            mainPov1.SetPOV(player1);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(player1.IsUsedInPov);
            
            // Step 2: Try to swap with preview POV (should fail)
            bool swapResult = mainPov1.Swap(previewPov1);
            Assert.False(swapResult);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(previewPov1.IsEmpty);
            
            // Step 3: Swap with another main POV
            swapResult = mainPov1.Swap(mainPov2);
            Assert.True(swapResult);
            Assert.True(mainPov1.IsEmpty);
            Assert.Equal("Player1", mainPov2.DisplayedPlayer);
            
            // Step 4: Set player2 to preview POV
            previewPov1.SetPOV(player2);
            Assert.Equal("Player2", previewPov1.DisplayedPlayer);
            Assert.True(player2.IsUsedInPreview);
            Assert.False(player2.IsUsedInPov);
        }

        [Fact]
        public void OnPOVClick_PlayerAlreadyUsed_ShouldNotChange()
        {
            // Simulate: Try to set a player that's already used in another POV
            PointOfView pov1 = CreatePOV(1);
            PointOfView pov2 = CreatePOV(2);
            IPlayer player = CreatePlayer("Player1");
            
            // Set player to pov1
            pov1.SetPOV(player);
            Assert.True(player.IsUsedInPov);
            
            // Try to set same player to pov2 (should keep old player)
            player.IsUsedInPov = true; // Simulate player already in use
            pov2.SetPOV(player);
            
            // Since SetPlayerToPOV checks IsPlayerUsed, it should not change
            Assert.True(pov2.IsEmpty || pov2.DisplayedPlayer != "Player1");
        }

        [Theory]
        [InlineData(SceneType.Main, SceneType.Main, true)]
        [InlineData(SceneType.Preview, SceneType.Preview, true)]
        [InlineData(SceneType.Main, SceneType.Preview, false)]
        [InlineData(SceneType.Preview, SceneType.Main, false)]
        public void OnPOVClick_SwapCustomStreams_BetweenScenes(SceneType scene1, SceneType scene2, bool shouldSucceed)
        {
            // Simulate: Swap custom streams between different scene types
            PointOfView pov1 = CreatePOV(1, scene1);
            PointOfView pov2 = CreatePOV(2, scene2);
            
            // Set custom streams
            pov1.CustomStreamName = "CustomStream1";
            pov1.CustomStreamType = StreamType.twitch;
            pov1.SetCustomPOV();
            
            pov2.CustomStreamName = "CustomStream2";
            pov2.CustomStreamType = StreamType.youtube;
            pov2.SetCustomPOV();
            
            // Try to swap
            bool result = pov1.Swap(pov2);
            
            Assert.Equal(shouldSucceed, result);
            if (shouldSucceed)
            {
                Assert.Equal("CustomStream2", pov1.DisplayedPlayer);
                Assert.Equal("CustomStream1", pov2.DisplayedPlayer);
                Assert.Equal(StreamType.youtube, pov1.CustomStreamType);
                Assert.Equal(StreamType.twitch, pov2.CustomStreamType);
            }
        }

        [Fact]
        public void OnPOVClick_ClearAndReassign()
        {
            // Simulate: Clear POV and reassign new player
            PointOfView pov = CreatePOV(1);
            IPlayer player1 = CreatePlayer("Player1");
            IPlayer player2 = CreatePlayer("Player2");
            
            // Set first player
            pov.SetPOV(player1);
            Assert.Equal("Player1", pov.DisplayedPlayer);
            
            // Clear
            pov.Clear(fullClear: true);
            Assert.True(pov.IsEmpty);
            Assert.False(player1.IsUsedInPov);

            // Set new player
            pov.SetPOV(player2);
            Assert.Equal("Player2", pov.DisplayedPlayer);
            Assert.True(player2.IsUsedInPov);
        }

        [Fact]
        public void OnPOVClick_MixedWhitelistedAndCustomPlayers()
        {
            // Simulate: Mix of whitelisted and custom players
            PointOfView pov1 = CreatePOV(1);
            PointOfView pov2 = CreatePOV(2);
            IPlayer whitelistedPlayer = CreatePlayer("WhitelistedPlayer");
            IPlayer customPlayer = CreatePlayer("CustomPlayer", false);
            
            // Set whitelisted player
            pov1.SetPOV(whitelistedPlayer);
            Assert.Equal("WhitelistedPlayer", pov1.DisplayedPlayer);
            Assert.Equal(string.Empty, pov1.CustomStreamName); // Should not set custom stream
            
            // Set non-whitelisted player (creates custom POV)
            pov2.SetPOV(customPlayer);
            Assert.Equal("CustomPlayer", pov2.DisplayedPlayer);
            Assert.Equal("CustomPlayer", pov2.CustomStreamName); // Should set custom stream
            
            // Swap them
            bool swapResult = pov1.Swap(pov2);
            Assert.True(swapResult);
            Assert.Equal("CustomPlayer", pov1.DisplayedPlayer);
            Assert.Equal("WhitelistedPlayer", pov2.DisplayedPlayer);
        }
    }
}