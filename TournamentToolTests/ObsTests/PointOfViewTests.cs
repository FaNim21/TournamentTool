using Moq;
using TournamentTool.Enums;
using TournamentTool.Models;
using TournamentTool.Modules.Controller;
using TournamentTool.Modules.OBS;

namespace TournamentToolTests.ObsTests;

public class PointOfViewTests
{
    private readonly Mock<IPointOfViewOBSController> _mockController;
    private readonly PointOfViewOBSData _testData;
    private readonly PointOfView _sut;

    protected PointOfViewTests()
    {
        _mockController = new Mock<IPointOfViewOBSController>();
        _testData = new PointOfViewOBSData(1, "TestGroup", "TestScene", "TestSceneItem");
        _sut = new PointOfView(_mockController.Object, _testData)
        {
            OriginWidth = 1920,
            OriginHeight = 1080,
            OriginX = 100,
            OriginY = 200
        };
    }

    public class CoreFunctionalityTests : PointOfViewTests
    {
        [Fact]
        public void Constructor_InitializesWithCorrectDefaults()
        {
            Assert.Equal(_testData, _sut.Data);
            Assert.NotNull(_sut.ApplyVolumeCommand);
            Assert.NotNull(_sut.RefreshCommand);
            Assert.False(_sut.IsFocused);
            Assert.True(_sut.IsEmpty);
        }

        [Theory]
        [InlineData(2.0f, 50, 100, 960, 540)]
        [InlineData(4.0f, 25, 50, 480, 270)]
        public void UpdateTransform_CalculatesCorrectDimensions(float proportion, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
        {
            _sut.UpdateTransform(proportion);

            Assert.Equal(expectedX, _sut.X);
            Assert.Equal(expectedY, _sut.Y);
            Assert.Equal(expectedWidth, _sut.Width);
            Assert.Equal(expectedHeight, _sut.Height);
        }

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
            Assert.Null(_sut.player);
            Assert.Equal(fullClear ? string.Empty : "CustomStream", _sut.CustomStreamName);
            _mockController.Verify(c => c.Clear(_testData), Times.Once);
        }
    }

    public class VolumeTests : PointOfViewTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(50, false)]
        [InlineData(100, false)]
        public void ApplyVolume_UpdatesVolumeAndMutedState(int volume, bool expectedMuted)
        {
            _sut.NewVolume = volume;

            _sut.ApplyVolume();

            Assert.Equal(volume, _sut.Volume);
            Assert.Equal(expectedMuted, _sut.IsMuted);
            _mockController.Verify(c => c.UpdatePOVBrowser(_testData), Times.Once);
        }

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

    public class SetPOVTests : PointOfViewTests
    {
        private Mock<IPlayer> CreateMockPlayer(bool isFromWhitelist = true, string displayName = "TestPlayer")
        {
            var player = new Mock<IPlayer>();
            player.Setup(p => p.DisplayName).Returns(displayName);
            player.Setup(p => p.StreamDisplayInfo).Returns(new StreamDisplayInfo("TestStream", StreamType.twitch));
            player.Setup(p => p.HeadViewParameter).Returns("HeadParam");
            player.Setup(p => p.GetPersonalBest).Returns("1:00:00");
            player.Setup(p => p.IsFromWhitelist).Returns(isFromWhitelist);
            player.SetupProperty(p => p.IsUsedInPov);
            player.SetupProperty(p => p.IsUsedInPreview);
            return player;
        }

        [Fact]
        public void SetPOV_WithNull_ClearsEverything()
        {
            _sut.SetPOV(null);

            Assert.True(_sut.IsEmpty);
            _mockController.Verify(c => c.Clear(_testData), Times.Once);
        }

        [Fact]
        public void SetPOV_WithWhitelistedPlayer_SetsDirectly()
        {
            var player = CreateMockPlayer(isFromWhitelist: true);

            _sut.SetPOV(player.Object);

            Assert.Equal("TestPlayer", _sut.DisplayedPlayer);
            Assert.False(_sut.IsEmpty);
            _mockController.Verify(c => c.SendOBSInformations(_testData), Times.Once);
        }

        [Fact]
        public void SetPOV_WithNonWhitelistedPlayer_CreatesCustomPOV()
        {
            var player = CreateMockPlayer(isFromWhitelist: false);

            _sut.SetPOV(player.Object);

            Assert.Equal("TestStream", _sut.CustomStreamName);
            Assert.Equal("TestStream", _sut.DisplayedPlayer);
            _mockController.Verify(c => c.SendOBSInformations(_testData), Times.Once);
        }

        [Theory]
        [InlineData(SceneType.Main, true, false)]
        [InlineData(SceneType.Preview, false, true)]
        public void SetPOV_UpdatesCorrectUsageFlag(SceneType sceneType, bool expectedUsedInPov, bool expectedUsedInPreview)
        {
            var pov = new PointOfView(_mockController.Object, _testData, sceneType);
            var player = CreateMockPlayer();

            pov.SetPOV(player.Object);

            Assert.Equal(expectedUsedInPov, player.Object.IsUsedInPov);
            Assert.Equal(expectedUsedInPreview, player.Object.IsUsedInPreview);
        }
    }

    public class SetCustomPOVTests : PointOfViewTests
    {
        [Fact]
        public void SetCustomPOV_WithEmptyName_ClearsIfPreviouslySet()
        {
            _sut.CustomStreamName = "PreviousStream";
            _sut.SetCustomPOV();
            
            _sut.CustomStreamName = "";
            _sut.SetCustomPOV();

            Assert.Equal(string.Empty, _sut.CurrentCustomStreamName);
            _mockController.Verify(c => c.Clear(_testData), Times.Once);
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
            _mockController.Verify(c => c.SendOBSInformations(_testData), Times.Once);
        }

        [Fact]
        public void SetCustomPOV_SkipsIfNoChange()
        {
            _sut.CustomStreamName = "Stream";
            _sut.CustomStreamType = StreamType.twitch;
            _sut.SetCustomPOV();
            
            _sut.CustomStreamName = "Stream";
            _sut.CustomStreamType = StreamType.twitch;

            _sut.SetCustomPOV();

            _mockController.Verify(c => c.SendOBSInformations(It.IsAny<PointOfViewOBSData>()), Times.Once);
        }
    }

    public class SwapTests : PointOfViewTests
    {
        [Fact]
        public void Swap_WithNull_ReturnsFalse()
        {
            var result = _sut.Swap(null);

            Assert.False(result);
        }

        [Fact]
        public void Swap_WithDifferentSceneType_ReturnsFalse()
        {
            var otherPov = new PointOfView(_mockController.Object, new PointOfViewOBSData(2, "G2", "S2", "I2"), SceneType.Preview);

            var result = _sut.Swap(otherPov);

            Assert.False(result);
        }

        [Fact]
        public void Swap_WithSameSceneType_SwapsSuccessfully()
        {
            var otherData = new PointOfViewOBSData(2, "G2", "S2", "I2");
            var otherPov = new PointOfView(_mockController.Object, otherData, SceneType.Main);
            
            _sut.CustomStreamName = "Stream1";
            otherPov.CustomStreamName = "Stream2";

            var result = _sut.Swap(otherPov);

            Assert.True(result);
            Assert.Equal("Stream2", _sut.CustomStreamName);
            Assert.Equal("Stream1", otherPov.CustomStreamName);
        }
    }

    public class OnPOVClickSimulationTests : PointOfViewTests
    {
        private PointOfView CreatePOV(int id, SceneType sceneType = SceneType.Main)
        {
            var data = new PointOfViewOBSData(id, $"Group{id}", $"Scene{id}", $"Item{id}");
            return new PointOfView(_mockController.Object, data, sceneType);
        }

        private Mock<IPlayer> CreatePlayer(string name, bool isFromWhitelist = true)
        {
            var player = new Mock<IPlayer>();
            player.Setup(p => p.DisplayName).Returns(name);
            player.Setup(p => p.StreamDisplayInfo).Returns(new StreamDisplayInfo(name + "Stream", StreamType.twitch));
            player.Setup(p => p.IsFromWhitelist).Returns(isFromWhitelist);
            player.SetupProperty(p => p.IsUsedInPov);
            player.SetupProperty(p => p.IsUsedInPreview);
            return player;
        }

        [Fact]
        public void OnPOVClick_SamePOV_ShouldDeselect()
        {
            // Simulate: Click on already selected POV
            var pov = CreatePOV(1);
            pov.Focus();
            
            // Simulate second click on same POV
            pov.UnFocus();
            
            Assert.False(pov.IsFocused);
        }

        [Fact]
        public void OnPOVClick_DifferentEmptyPOVs_ShouldFocusNew()
        {
            // Simulate: Click from one empty POV to another
            var pov1 = CreatePOV(1);
            var pov2 = CreatePOV(2);
            
            pov1.Focus();
            Assert.True(pov1.IsFocused);
            
            pov1.UnFocus();
            pov2.Focus();
            
            Assert.False(pov1.IsFocused);
            Assert.True(pov2.IsFocused);
        }

        [Fact]
        public void OnPOVClick_SwapBetweenPOVsWithPlayers()
        {
            // Simulate: Swap between two POVs with players
            var pov1 = CreatePOV(1);
            var pov2 = CreatePOV(2);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            pov1.SetPOV(player1.Object);
            pov2.SetPOV(player2.Object);
            
            var swapResult = pov1.Swap(pov2);
            
            Assert.True(swapResult);
            Assert.Equal("Player2", pov1.DisplayedPlayer);
            Assert.Equal("Player1", pov2.DisplayedPlayer);
        }

        [Fact]
        public void OnPOVClick_SetPlayerToPOV()
        {
            // Simulate: Click on POV with a player selected
            var pov = CreatePOV(1);
            var player = CreatePlayer("TestPlayer");
            
            pov.SetPOV(player.Object);
            
            Assert.Equal("TestPlayer", pov.DisplayedPlayer);
            Assert.True(player.Object.IsUsedInPov);
        }

        [Theory]
        [InlineData(SceneType.Main, SceneType.Preview, false)] // Different scene types - no swap
        [InlineData(SceneType.Main, SceneType.Main, true)]     // Same scene type - swap allowed
        [InlineData(SceneType.Preview, SceneType.Preview, true)] // Same scene type - swap allowed
        public void OnPOVClick_BetweenDifferentSceneTypes(SceneType scene1Type, SceneType scene2Type, bool shouldSwap)
        {
            // Simulate: Interaction between POVs from different scenes
            var pov1 = CreatePOV(1, scene1Type);
            var pov2 = CreatePOV(2, scene2Type);
            var player = CreatePlayer("Player1");
            
            pov1.SetPOV(player.Object);
            
            var swapResult = pov1.Swap(pov2);
            
            Assert.Equal(shouldSwap, swapResult);
            if (shouldSwap)
            {
                Assert.Equal("Player1", pov2.DisplayedPlayer);
                Assert.True(pov2.IsEmpty == false);
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
            var mainPov1 = CreatePOV(1, SceneType.Main);
            var mainPov2 = CreatePOV(2, SceneType.Main);
            var previewPov1 = CreatePOV(3, SceneType.Preview);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            // Step 1: Set player1 to mainPov1
            mainPov1.SetPOV(player1.Object);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(player1.Object.IsUsedInPov);
            
            // Step 2: Try to swap with preview POV (should fail)
            var swapResult = mainPov1.Swap(previewPov1);
            Assert.False(swapResult);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(previewPov1.IsEmpty);
            
            // Step 3: Swap with another main POV
            swapResult = mainPov1.Swap(mainPov2);
            Assert.True(swapResult);
            Assert.True(mainPov1.IsEmpty);
            Assert.Equal("Player1", mainPov2.DisplayedPlayer);
            
            // Step 4: Set player2 to preview POV
            previewPov1.SetPOV(player2.Object);
            Assert.Equal("Player2", previewPov1.DisplayedPlayer);
            Assert.True(player2.Object.IsUsedInPreview);
            Assert.False(player2.Object.IsUsedInPov);
        }

        [Fact]
        public void OnPOVClick_PlayerAlreadyUsed_ShouldNotChange()
        {
            // Simulate: Try to set a player that's already used in another POV
            var pov1 = CreatePOV(1);
            var pov2 = CreatePOV(2);
            var player = CreatePlayer("Player1");
            
            // Set player to pov1
            pov1.SetPOV(player.Object);
            Assert.True(player.Object.IsUsedInPov);
            
            // Try to set same player to pov2 (should keep old player)
            player.Object.IsUsedInPov = true; // Simulate player already in use
            pov2.SetPOV(player.Object);
            
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
            var pov1 = CreatePOV(1, scene1);
            var pov2 = CreatePOV(2, scene2);
            
            // Set custom streams
            pov1.CustomStreamName = "CustomStream1";
            pov1.CustomStreamType = StreamType.twitch;
            pov1.SetCustomPOV();
            
            pov2.CustomStreamName = "CustomStream2";
            pov2.CustomStreamType = StreamType.youtube;
            pov2.SetCustomPOV();
            
            // Try to swap
            var result = pov1.Swap(pov2);
            
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
            var pov = CreatePOV(1);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            // Set first player
            pov.SetPOV(player1.Object);
            Assert.Equal("Player1", pov.DisplayedPlayer);
            
            // Clear
            pov.Clear(fullClear: true);
            Assert.True(pov.IsEmpty);
            Assert.False(player1.Object.IsUsedInPov);
            
            // Set new player
            pov.SetPOV(player2.Object);
            Assert.Equal("Player2", pov.DisplayedPlayer);
            Assert.True(player2.Object.IsUsedInPov);
        }

        [Fact]
        public void OnPOVClick_FocusUnfocusSequence()
        {
            // Simulate: Focus/unfocus sequence during clicks
            var pov1 = CreatePOV(1);
            var pov2 = CreatePOV(2);
            var pov3 = CreatePOV(3);
            
            // Click pov1
            pov1.Focus();
            Assert.True(pov1.IsFocused);
            
            // Click pov2 (pov1 should unfocus)
            pov1.UnFocus();
            pov2.Focus();
            Assert.False(pov1.IsFocused);
            Assert.True(pov2.IsFocused);
            
            // Click pov2 again (should unfocus - deselect)
            pov2.UnFocus();
            Assert.False(pov2.IsFocused);
            
            // Click pov3
            pov3.Focus();
            Assert.True(pov3.IsFocused);
        }

        [Fact]
        public void OnPOVClick_MixedWhitelistedAndCustomPlayers()
        {
            // Simulate: Mix of whitelisted and custom players
            var pov1 = CreatePOV(1);
            var pov2 = CreatePOV(2);
            var whitelistedPlayer = CreatePlayer("WhitelistedPlayer", isFromWhitelist: true);
            var customPlayer = CreatePlayer("CustomPlayer", isFromWhitelist: false);
            
            // Set whitelisted player
            pov1.SetPOV(whitelistedPlayer.Object);
            Assert.Equal("WhitelistedPlayer", pov1.DisplayedPlayer);
            Assert.Equal(string.Empty, pov1.CustomStreamName); // Should not set custom stream
            
            // Set non-whitelisted player (creates custom POV)
            pov2.SetPOV(customPlayer.Object);
            Assert.Equal("CustomPlayerStream", pov2.DisplayedPlayer);
            Assert.Equal("CustomPlayerStream", pov2.CustomStreamName); // Should set custom stream
            
            // Swap them
            var swapResult = pov1.Swap(pov2);
            Assert.True(swapResult);
            Assert.Equal("CustomPlayerStream", pov1.DisplayedPlayer);
            Assert.Equal("WhitelistedPlayer", pov2.DisplayedPlayer);
        }
    }
}