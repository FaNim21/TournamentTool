using Meziantou.Xunit;
using Moq;
using MultiOpener.Entities.Interfaces;
using TournamentTool.Enums;
using TournamentTool.Interfaces;
using TournamentTool.Managers;
using TournamentTool.Models;
using TournamentTool.Modules;
using TournamentTool.Modules.Logging;
using TournamentTool.Modules.OBS;
using TournamentTool.Services;
using TournamentTool.Services.Background;
using TournamentTool.Utils;
using TournamentTool.ViewModels.Entities;
using TournamentTool.ViewModels.Selectable;

namespace TournamentToolTests.ViewModelsTests;

public class TestLogger : ILoggingService
{
    public void Log(object message, LogLevel level = LogLevel.Normal) { }
    public void Error(object message) { }
    public void Warning(object message) { }
    public void Information(object message) { }
    public void Debug(object message) { }
}

public class PresetManagerTests
{
    private readonly Mock<IPresetSaver> _mockPresetService;
    private readonly Mock<TournamentViewModel> _mockTournamentViewModel;
    private readonly Mock<MainViewModelCoordinator> _mockCoordinator;
    private readonly PresetManagerViewModel _viewModel;

    
    public PresetManagerTests()
    {
        Consts.IsTesting = true;
        
        _mockPresetService = new Mock<IPresetSaver>();
        _mockTournamentViewModel = new Mock<TournamentViewModel>();
        _mockCoordinator = new Mock<MainViewModelCoordinator>(null!);
        
        var mockBackgroundCoordinator = new Mock<IBackgroundCoordinator>();
        var mockSettingsService = new Mock<SettingsService>();
        var luaScriptManager = new Mock<ILuaScriptsManager>();

        _viewModel = new PresetManagerViewModel(_mockCoordinator.Object, _mockTournamentViewModel.Object, _mockPresetService.Object, mockBackgroundCoordinator.Object, new TestLogger(), mockSettingsService.Object, luaScriptManager.Object);
    }

    [Fact]
    public void AddItem_AddsPresetToCollection()
    {
        var preset = new TournamentPreset("TestPreset");
        _viewModel.AddItem(preset);

        Assert.Contains(preset, _viewModel.Presets);
    }

    [Fact]
    public void RemoveItem_RemovesPresetFromCollection()
    {
        var preset = new TournamentPreset("TestPreset");
        _viewModel.AddItem(preset);
        _viewModel.RemoveItem(preset);
        
        Assert.DoesNotContain(preset, _viewModel.Presets);
    }

    [Fact]
    public void IsPresetNameUnique_ReturnsTrueForUniqueName()
    {
        var preset1 = new TournamentPreset("Preset1");
        var preset2 = new TournamentPreset("NameUnique");
        _viewModel.AddItem(preset1);
        _viewModel.AddItem(preset2);
        
        Assert.True(_viewModel.IsPresetNameUnique("UniqueName"));
    }

    [Fact]
    public void IsPresetNameUnique_ReturnsFalseForDuplicateName()
    {
        var preset = new TournamentPreset("Duplicate");
        _viewModel.AddItem(preset);

        Assert.False(_viewModel.IsPresetNameUnique("Duplicate"));
    }

    [Fact]
    public void CurrentChosen_SetUpdatesTournamentViewModel()
    {
        var preset = new TournamentPreset("Preset1");

        _viewModel.CurrentChosen = preset;

        Assert.True(_mockTournamentViewModel.Object.IsCurrentlyOpened);
    }

    [Fact]
    public void CurrentChosen_SetToNull_ClosesTournament()
    {
        _viewModel.CurrentChosen = null;

        Assert.False(_mockTournamentViewModel.Object.IsCurrentlyOpened);
    }

    
    [DisableParallelization]
    public class PresetCommands
    {
        private readonly PresetManagerViewModel _viewModel;
        private readonly PresetService _presetService;
        
        private readonly Mock<TournamentViewModel> _mockTournamentViewModel;
        private readonly Mock<ICoordinator> _mockCoordinator;


        public PresetCommands()
        {
            Consts.IsTesting = true;
            
            _mockTournamentViewModel = new Mock<TournamentViewModel>();
            _mockCoordinator = new Mock<ICoordinator>();
            _presetService = new PresetService(_mockTournamentViewModel.Object);
            
            Mock<IBackgroundCoordinator> _mockBackgroundCoordinator = new Mock<IBackgroundCoordinator>();
            var mockSettingsService = new Mock<SettingsService>();
            var luaScriptManager = new Mock<ILuaScriptsManager>();

            _viewModel = new PresetManagerViewModel(_mockCoordinator.Object, _mockTournamentViewModel.Object, _presetService, _mockBackgroundCoordinator.Object, new TestLogger(), mockSettingsService.Object, luaScriptManager.Object);
        }

        [Fact]
        public void SavePresetCommand_SavesPresetCorrectly()
        {
            Tournament tournament = new Tournament { Name = "_SaveTest" };
            string path = ((IPreset)tournament).GetPath();
            if (File.Exists(path)) File.Delete(path);
            
            _mockTournamentViewModel.Object.ChangeData(tournament);
            
            _viewModel.SavePresetCommand.Execute(null);
            
            bool fileExist = File.Exists(path);
            Assert.True(fileExist);
        }

        [Fact]
        public void OpenControllerCommand_ChangesToCorrectViewModel()
        {
            _viewModel.OpenControllerCommand.Execute(null);
            _mockCoordinator.Verify(c => c.SelectViewModel("Controller"), Times.Once);
        }

        [Fact]
        public void OpenLeaderboardCommand_ChangesToCorrectViewModel()
        {
            _viewModel.OpenLeaderboardCommand.Execute(null);
            _mockCoordinator.Verify(c => c.SelectViewModel("Leaderboard"), Times.Once);
        }

        [Fact]
        public void AddNewPresetCommand_CorrectlyAddsPreset()
        {
            _viewModel.Presets.Clear();
            string path = Path.Combine(Consts.PresetsPath, "New Preset.json");
            if (File.Exists(path)) File.Delete(path);
            
            _viewModel.AddNewPresetCommand.Execute(null);

            Assert.Contains(_viewModel.Presets, p => p.Name.Equals("New Preset"));
            Assert.Equal("New Preset", _viewModel.Presets.Last().Name);
        }

        [Fact]
        public void ClearCurrentPresetCommand_ClearingAllVariables()
        {
            Tournament tournament = new Tournament
            {
                Name = "_ClearTest",
                
                IsUsingTeamNames = true,
                IsUsingWhitelistOnPaceMan = false,
                
                SceneCollection = "asdf",
                
                SetPovPBText = true,
                SetPovHeadsInBrowser = true,
                DisplayedNameType = DisplayedNameType.WhiteList,
                
                IsUsingTwitchAPI = true,
                ShowStreamCategory = false,
                
                PaceManRefreshRateMiliseconds = 10000,
                
                Structure2GoodPaceMiliseconds = 20,
                FirstPortalGoodPaceMiliseconds = 20,
                EnterEndGoodPaceMiliseconds = 20,
                CreditsGoodPaceMiliseconds = 20,
                EnterStrongholdGoodPaceMiliseconds = 20,
                
                ControllerMode = ControllerMode.Ranked,
                
                RankedApiKey = "asdf",
                RankedApiPlayerName = "asdf"
            };

            _mockTournamentViewModel.Object.ChangeData(tournament);
            _viewModel.SavePresetCommand.Execute(null);
            
            _mockTournamentViewModel.Object.Clear();
            var tournamentCleared = _mockTournamentViewModel.Object.GetData();
            
            Assert.False(tournamentCleared.IsUsingTeamNames);
            Assert.True(tournamentCleared.IsUsingWhitelistOnPaceMan);
    
            Assert.Equal(string.Empty, tournamentCleared.SceneCollection);

            Assert.False(tournamentCleared.SetPovHeadsInBrowser);
            Assert.False(tournamentCleared.SetPovPBText);
            Assert.Equal(DisplayedNameType.None, tournamentCleared.DisplayedNameType);

            Assert.False(tournamentCleared.IsUsingTwitchAPI);
            Assert.True(tournamentCleared.ShowStreamCategory);

            Assert.Equal(3000, tournamentCleared.PaceManRefreshRateMiliseconds);

            Assert.Equal(270000, tournamentCleared.Structure2GoodPaceMiliseconds);
            Assert.Equal(360000, tournamentCleared.FirstPortalGoodPaceMiliseconds);
            Assert.Equal(450000, tournamentCleared.EnterStrongholdGoodPaceMiliseconds);
            Assert.Equal(480000, tournamentCleared.EnterEndGoodPaceMiliseconds);
            Assert.Equal(600000, tournamentCleared.CreditsGoodPaceMiliseconds);

            Assert.Equal(ControllerMode.None, tournamentCleared.ControllerMode);

            Assert.Equal(string.Empty, tournamentCleared.RankedApiKey);
            Assert.Equal(string.Empty, tournamentCleared.RankedApiPlayerName);
        }

        [Fact]
        public void DuplicateCurrentPresetCommand_CorrectlyDuplicates()
        {
            for (int i = 0; i < _viewModel.Presets.Count; i++)
            {
                var current = _viewModel.Presets[i];
                if (!current.Name.EndsWith(')')) continue;
                
                _viewModel.RemoveItem(current);
                _viewModel.TournamentViewModel.Delete();
                File.Delete(current.GetPath());
                i--;
            }
            
            _viewModel.CurrentChosen = _viewModel.Presets[0];
            string expectedName = _viewModel.CurrentChosen.Name + " (1)";
            
            _viewModel.DuplicateCurrentPresetCommand.Execute(_viewModel.CurrentChosen);
            string name = _viewModel.Presets[^1].Name;

            Assert.Equal(expectedName, name);
        }

        [Fact]
        public void RenameItemCommand_CorrectlyRenaming()
        {
            string renamedPath = Path.Combine(Consts.PresetsPath, "_RenameTest_After.json");
            if (File.Exists(renamedPath))
            {
                File.Delete(renamedPath);
                _viewModel.Presets.Clear();
            }
            
            TournamentPreset tournament = new TournamentPreset("_RenameTest");
            string original = tournament.GetPath();
            _viewModel.AddItem(tournament);
            _viewModel.CurrentChosen = tournament;

            IRenameItem renameItem = _viewModel.CurrentChosen;
            renameItem.ChangeName("_RenameTest_After");

            Assert.True(File.Exists(renamedPath));
            Assert.False(File.Exists(original));
        }

        [Fact]
        public void RemoveCurrentPresetCommand_CorrectlyRemovesPreset()
        {
            TournamentPreset tournament = new TournamentPreset("_RemoveTest");
            string path = ((IPreset)tournament).GetPath();
            if (!File.Exists(path))
            {
                _viewModel.AddItem(tournament);
                _viewModel.CurrentChosen = tournament;
            }

            TournamentPreset preset = null!;
            for (int i = 0; i < _viewModel.Presets.Count; i++)
            {
                var current = _viewModel.Presets[i];
                if (current.Name.Equals("_RemoveTest")) preset = current;
            }
            _viewModel.CurrentChosen = preset;
            Assert.NotNull(_viewModel.CurrentChosen);
            Assert.True(_mockTournamentViewModel.Object.IsCurrentlyOpened);
            
            _viewModel.RemoveItem(preset!);
            
            bool fileExist = File.Exists(path);
            Assert.False(fileExist);
            Assert.True(_mockTournamentViewModel.Object.HasBeenRemoved);
        }
    }
}