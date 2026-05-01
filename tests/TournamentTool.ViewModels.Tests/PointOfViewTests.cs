using NSubstitute;
using ObsWebSocket.Core.Protocol.Common;
using TournamentTool.Core.Interfaces;
using TournamentTool.Domain.Entities;
using TournamentTool.Domain.Enums;
using TournamentTool.Services.Logging;
using TournamentTool.ViewModels.Obs;
using TournamentTool.ViewModels.Obs.Items;
using TournamentTool.ViewModels.Selectable.Controller;

namespace TournamentTool.ViewModels.Tests;

public class PointOfViewViewModelTests
{
    private readonly ISceneCanvasViewModel _canvasViewModel = Substitute.For<ISceneCanvasViewModel>();
    private readonly IDispatcherService _dispatcher = Substitute.For<IDispatcherService>();
    private readonly ILoggingService _logger = Substitute.For<ILoggingService>();
    private readonly IScene _scene = Substitute.For<IScene>();
    
    private readonly PointOfViewViewModel _sut;
    
    
    protected PointOfViewViewModelTests()
    {
        _sut = CreatePOV(1234, SceneType.Main, false).GetAwaiter().GetResult();
    }

    public async Task<PointOfViewViewModel> CreatePOV(int id, SceneType type = SceneType.Main, bool useGroup = true)
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
        
        var pov = new PointOfViewViewModel(_canvasViewModel, _dispatcher, _logger, type);
        await pov.Initialize(_scene, false, true, item, group);

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
        [Fact]
        public void Constructor_InitializesWithCorrectDefaults()
        {
            Assert.NotNull(_sut.ApplyVolumeCommand);
            Assert.NotNull(_sut.RefreshCommand);
            Assert.False(_sut.IsFocused);
            Assert.True(_sut.IsEmpty);
        }

        //TODO: 0 Testy dla transform powinny byc w oddzielnej klasie
        [Theory]
        [InlineData(2.0f, 50, 100, 960, 540)]
        [InlineData(4.0f, 25, 50, 480, 270)]
        public void UpdateTransform_CalculatesCorrectDimensions(float proportion, int expectedX, int expectedY, int expectedWidth, int expectedHeight)
        {
            _sut.Transform.UpdateProportions(proportion);

            Assert.Equal(expectedX, _sut.Transform.X);
            Assert.Equal(expectedY, _sut.Transform.Y);
            Assert.Equal(expectedWidth, _sut.Transform.Width);
            Assert.Equal(expectedHeight, _sut.Transform.Height);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Clear_ResetsStateCorrectly(bool fullClear)
        {
            _sut.DisplayedPlayer = "Player";
            _sut.Volume = 50;
            _sut.CustomStreamName = "CustomStream";

            await _sut.Clear(fullClear);

            Assert.Equal(string.Empty, _sut.DisplayedPlayer);
            Assert.Equal(0, _sut.Volume);
            Assert.Null(_sut.player);
            Assert.Equal(fullClear ? string.Empty : "CustomStream", _sut.CustomStreamName);
        }
    }

    public class VolumeViewModelTests : PointOfViewViewModelTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(50, false)]
        [InlineData(100, false)]
        public async Task ApplyVolume_UpdatesVolumeAndMutedState(int volume, bool expectedMuted)
        {
            _sut.NewVolume = volume;

            await _sut.ApplyVolume();

            Assert.Equal(volume, _sut.Volume);
            Assert.Equal(expectedMuted, _sut.IsMuted);
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
        public async Task SetPOV_WithNull_ClearsEverything()
        {
            _sut.player = CreateMockPlayer();
            
            await _sut.SetPOV(null);
            
            Assert.True(_sut.IsEmpty);
        }

        [Fact]
        public async Task SetPOV_WithWhitelistedPlayer_SetsDirectly()
        {
            var player = CreateMockPlayer(isFromWhitelist: true);

            await _sut.SetPOV(player);

            Assert.Equal("TestPlayer", _sut.DisplayedPlayer);
            Assert.False(_sut.IsEmpty);
        }

        [Fact]
        public async Task SetPOV_WithNonWhitelistedPlayer_CreatesCustomPOV()
        {
            var player = CreateMockPlayer(isFromWhitelist: false);

            await _sut.SetPOV(player);

            Assert.Equal("TestStream", _sut.CustomStreamName);
            Assert.Equal("TestStream", _sut.DisplayedPlayer);
        }

        [Theory]
        [InlineData(SceneType.Main, true, false)]
        [InlineData(SceneType.Preview, false, true)]
        public async Task SetPOV_UpdatesCorrectUsageFlag(SceneType sceneType, bool expectedUsedInPov, bool expectedUsedInPreview)
        {
            var pov = await CreatePOV(1, sceneType);
            IPlayer player = CreateMockPlayer();

            await pov.SetPOV(player);

            Assert.Equal(expectedUsedInPov, player.IsUsedInPov);
            Assert.Equal(expectedUsedInPreview, player.IsUsedInPreview);
        }
    }

    public class SetCustomPovViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public async Task SetCustomPOV_WithEmptyName_ClearsIfPreviouslySet()
        {
            _sut.CustomStreamName = "PreviousStream";
            await _sut.SetCustomPOV();
            
            _sut.CustomStreamName = "";
            await _sut.SetCustomPOV();

            Assert.Equal(string.Empty, _sut.CurrentCustomStreamName);
        }

        [Theory]
        [InlineData("NewStream", StreamType.twitch)]
        [InlineData("YouTubeStream", StreamType.youtube)]
        public async Task SetCustomPOV_CreatesCustomPlayer(string streamName, StreamType streamType)
        {
            _sut.CustomStreamName = streamName;
            _sut.CustomStreamType = streamType;

            await _sut.SetCustomPOV();

            Assert.Equal(streamName, _sut.DisplayedPlayer);
            Assert.Equal(streamName, _sut.CurrentCustomStreamName);
            Assert.Equal(streamType, _sut.CurrentCustomStreamType);
        }
    }

    public class SwapViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public async Task Swap_WithNull_ReturnsFalse()
        {
            var result = await _sut.Swap(null);

            Assert.False(result);
        }

        [Fact]
        public async Task Swap_WithDifferentSceneType_ReturnsFalse()
        {
            var otherPov = await CreatePOV(2, SceneType.Preview);
            
            var result = await _sut.Swap(otherPov);

            Assert.False(result);
        }

        [Fact]
        public async Task Swap_WithSameSceneType_SwapsSuccessfully()
        {
            var otherPov = await CreatePOV(2);
            
            _sut.CustomStreamName = "Stream1";
            otherPov.CustomStreamName = "Stream2";

            var result = await _sut.Swap(otherPov);

            Assert.True(result);
            Assert.Equal("Stream2", _sut.CustomStreamName);
            Assert.Equal("Stream1", otherPov.CustomStreamName);
        }
    }

    public class OnPovClickSimulationViewModelTests : PointOfViewViewModelTests
    {
        [Fact]
        public async Task OnPOVClick_SamePOV_ShouldDeselect()
        {
            // Simulate: Click on already selected POV
            var pov = await CreatePOV(1);
            pov.Focus();
            
            // Simulate second click on same POV
            pov.UnFocus();
            
            Assert.False(pov.IsFocused);
        }

        [Fact]
        public async Task OnPOVClick_DifferentEmptyPOVs_ShouldFocusNew()
        {
            // Simulate: Click from one empty POV to another
            var pov1 = await CreatePOV(1);
            var pov2 = await CreatePOV(2);
            
            pov1.Focus();
            Assert.True(pov1.IsFocused);
            
            pov1.UnFocus();
            pov2.Focus();
            
            Assert.False(pov1.IsFocused);
            Assert.True(pov2.IsFocused);
        }

        [Fact]
        public async Task OnPOVClick_SwapBetweenPOVsWithPlayers()
        {
            // Simulate: Swap between two POVs with players
            var pov1 = await CreatePOV(1);
            var pov2 = await CreatePOV(2);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            await pov1.SetPOV(player1);
            await pov2.SetPOV(player2);
            
            var swapResult = await pov1.Swap(pov2);
            
            Assert.True(swapResult);
            Assert.Equal("Player2", pov1.DisplayedPlayer);
            Assert.Equal("Player1", pov2.DisplayedPlayer);
        }

        [Fact]
        public async Task OnPOVClick_SetPlayerToPOV()
        {
            // Simulate: Click on POV with a player selected
            var pov = await CreatePOV(1);
            var player = CreatePlayer("TestPlayer");
            
            await pov.SetPOV(player);
            
            Assert.Equal("TestPlayer", pov.DisplayedPlayer);
            Assert.True(player.IsUsedInPov);
        }

        [Theory]
        [InlineData(SceneType.Main, SceneType.Preview, false)] // Different scene types - no swap
        [InlineData(SceneType.Main, SceneType.Main, true)]     // Same scene type - swap allowed
        [InlineData(SceneType.Preview, SceneType.Preview, true)] // Same scene type - swap allowed
        public async Task OnPOVClick_BetweenDifferentSceneTypes(SceneType scene1Type, SceneType scene2Type, bool shouldSwap)
        {
            // Simulate: Interaction between POVs from different scenes
            var pov1 = await CreatePOV(1, scene1Type);
            var pov2 = await CreatePOV(2, scene2Type);
            var player = CreatePlayer("Player1");
            
            await pov1.SetPOV(player);
            
            var swapResult = await pov1.Swap(pov2);
            
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
        public async Task OnPOVClick_ComplexScenario_MultipleClicks()
        {
            // Simulate complex scenario with multiple POVs and clicks
            var mainPov1 = await CreatePOV(1);
            var mainPov2 = await CreatePOV(2);
            var previewPov1 = await CreatePOV(3, SceneType.Preview);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            // Step 1: Set player1 to mainPov1
            await mainPov1.SetPOV(player1);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(player1.IsUsedInPov);
            
            // Step 2: Try to swap with preview POV (should fail)
            var swapResult = await mainPov1.Swap(previewPov1);
            Assert.False(swapResult);
            Assert.Equal("Player1", mainPov1.DisplayedPlayer);
            Assert.True(previewPov1.IsEmpty);
            
            // Step 3: Swap with another main POV
            swapResult = await mainPov1.Swap(mainPov2);
            Assert.True(swapResult);
            Assert.True(mainPov1.IsEmpty);
            Assert.Equal("Player1", mainPov2.DisplayedPlayer);
            
            // Step 4: Set player2 to preview POV
            await previewPov1.SetPOV(player2);
            Assert.Equal("Player2", previewPov1.DisplayedPlayer);
            Assert.True(player2.IsUsedInPreview);
            Assert.False(player2.IsUsedInPov);
        }

        [Fact]
        public async Task OnPOVClick_PlayerAlreadyUsed_ShouldNotChange()
        {
            // Simulate: Try to set a player that's already used in another POV
            var pov1 = await CreatePOV(1);
            var pov2 = await CreatePOV(2);
            var player = CreatePlayer("Player1");
            
            // Set player to pov1
            await pov1.SetPOV(player);
            Assert.True(player.IsUsedInPov);
            
            // Try to set same player to pov2 (should keep old player)
            player.IsUsedInPov = true; // Simulate player already in use
            await pov2.SetPOV(player);
            
            // Since SetPlayerToPOV checks IsPlayerUsed, it should not change
            Assert.True(pov2.IsEmpty || pov2.DisplayedPlayer != "Player1");
        }

        [Theory]
        [InlineData(SceneType.Main, SceneType.Main, true)]
        [InlineData(SceneType.Preview, SceneType.Preview, true)]
        [InlineData(SceneType.Main, SceneType.Preview, false)]
        [InlineData(SceneType.Preview, SceneType.Main, false)]
        public async Task OnPOVClick_SwapCustomStreams_BetweenScenes(SceneType scene1, SceneType scene2, bool shouldSucceed)
        {
            // Simulate: Swap custom streams between different scene types
            var pov1 = await CreatePOV(1, scene1);
            var pov2 = await CreatePOV(2, scene2);
            
            // Set custom streams
            pov1.CustomStreamName = "CustomStream1";
            pov1.CustomStreamType = StreamType.twitch;
            await pov1.SetCustomPOV();
            
            pov2.CustomStreamName = "CustomStream2";
            pov2.CustomStreamType = StreamType.youtube;
            await pov2.SetCustomPOV();
            
            // Try to swap
            var result = await pov1.Swap(pov2);
            
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
        public async Task OnPOVClick_ClearAndReassign()
        {
            // Simulate: Clear POV and reassign new player
            var pov = await CreatePOV(1);
            var player1 = CreatePlayer("Player1");
            var player2 = CreatePlayer("Player2");
            
            // Set first player
            await pov.SetPOV(player1);
            Assert.Equal("Player1", pov.DisplayedPlayer);
            
            // Clear
            await pov.Clear(fullClear: true);
            Assert.True(pov.IsEmpty);
            Assert.False(player1.IsUsedInPov);
            
            // Set new player
            await pov.SetPOV(player2);
            Assert.Equal("Player2", pov.DisplayedPlayer);
            Assert.True(player2.IsUsedInPov);
        }

        [Fact]
        public async Task OnPOVClick_FocusUnfocusSequence()
        {
            // Simulate: Focus/unfocus sequence during clicks
            var pov1 = await CreatePOV(1);
            var pov2 = await CreatePOV(2);
            var pov3 = await CreatePOV(3);
            
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
        public async Task OnPOVClick_MixedWhitelistedAndCustomPlayers()
        {
            // Simulate: Mix of whitelisted and custom players
            var pov1 = await CreatePOV(1);
            var pov2 = await CreatePOV(2);
            var whitelistedPlayer = CreatePlayer("WhitelistedPlayer");
            var customPlayer = CreatePlayer("CustomPlayer", false);
            
            // Set whitelisted player
            await pov1.SetPOV(whitelistedPlayer);
            Assert.Equal("WhitelistedPlayer", pov1.DisplayedPlayer);
            Assert.Equal(string.Empty, pov1.CustomStreamName); // Should not set custom stream
            
            // Set non-whitelisted player (creates custom POV)
            await pov2.SetPOV(customPlayer);
            Assert.Equal("CustomPlayer", pov2.DisplayedPlayer);
            Assert.Equal("CustomPlayer", pov2.CustomStreamName); // Should set custom stream
            
            // Swap them
            bool swapResult = await pov1.Swap(pov2);
            Assert.True(swapResult);
            Assert.Equal("CustomPlayer", pov1.DisplayedPlayer);
            Assert.Equal("WhitelistedPlayer", pov2.DisplayedPlayer);
        }
    }
}